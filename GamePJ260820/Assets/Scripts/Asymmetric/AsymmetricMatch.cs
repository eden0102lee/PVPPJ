using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePJ.Asymmetric
{
    public enum MatchPhase { Hunt, Choice, Demon, Escort, Finished }
    public enum GrowthPath { Hero, Adventurer, Legion }
    public sealed class MatchRules
    {
        // ASYMMETRIC_DESIGN §§3–6. Numeric balance values remain prototype drafts.
        public float HuntSeconds = 1800, FinalSeconds = 300;
        public float CloseQuestsAt = 720, EndQuestsAt = 1200, RespawnSeconds = 8;
        public int EscortScore = 100, SurvivalScore = 80, DemonLossScore = 40;
    }
    public sealed class Contestant
    {
        public int Id, Score, XP, RewardCount, RevivalTokens;
        public GrowthPath Path;
        public float Health = 200, MaxHealth = 200, Contribution, RespawnRemaining;
        public bool Eliminated;
        public int Level => 1 + XP / 100;
        public bool Alive => Health > 0 && !Eliminated;
    }

    // Pure C# match authority. No Unity clocks, UI state, or scene dependencies.
    // Later network work should call this on the server and replicate snapshots.
    public sealed class AsymmetricMatch
    {
        public readonly MatchRules Rules;
        public readonly List<Contestant> Players = new List<Contestant>();
        public MatchPhase Phase { get; private set; } = MatchPhase.Hunt;
        public float Remaining { get; private set; }
        public float HuntElapsed { get; private set; }
        public float BossHealth { get; private set; } = 2400;
        public float CrystalHealth { get; private set; } = 1800;
        public int Chooser { get; private set; } = -1;
        public int Demon { get; private set; } = -1;
        public int Carrier { get; private set; } = -1;
        public int Winner { get; private set; } = -1;
        public string Result { get; private set; } = "";
        public bool NewQuestsOpen => Phase == MatchPhase.Hunt && HuntElapsed < Rules.CloseQuestsAt;
        public bool QuestRewardsOpen => Phase == MatchPhase.Hunt && HuntElapsed < Rules.EndQuestsAt;
        public bool Running => Phase == MatchPhase.Hunt || Phase == MatchPhase.Demon || Phase == MatchPhase.Escort;
        public event Action<int> Respawned;
        public event Action<int> PlayerDied;
        public event Action<MatchPhase> PhaseChanged;

        public AsymmetricMatch(int count, MatchRules rules = null)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            Rules = rules ?? new MatchRules(); Remaining = Rules.HuntSeconds;
            for (int i = 0; i < count; i++) Players.Add(new Contestant { Id = i, Path = (GrowthPath)(i % 3) });
        }
        public void Tick(float seconds)
        {
            if (!Running || seconds <= 0) return;
            float dt = Math.Min(seconds, Remaining);
            Remaining = Math.Max(0, Remaining - dt);
            if (Phase == MatchPhase.Hunt) HuntElapsed += dt;
            foreach (var p in Players)
            {
                if (p.Alive || p.Eliminated) continue;
                p.RespawnRemaining -= dt;
                if (p.RespawnRemaining <= 0) Revive(p);
            }
            if (Remaining <= 0)
            {
                if (Phase == MatchPhase.Demon) Finish(Demon, "魔王存活至終局倒數結束");
                else Finish(-1, "魔神降臨：全員落敗");
            }
        }
        public bool CompleteQuest(int id, bool revival)
        {
            var p = Players[id];
            if (!QuestRewardsOpen || !p.Alive) return false;
            p.Score += 80; p.XP += 100; p.RewardCount++;
            if (revival) p.RevivalTokens++;
            p.MaxHealth = 200 + (p.Level - 1) * (p.Path == GrowthPath.Hero ? 35 : 15);
            p.Health = Math.Min(p.MaxHealth, p.Health + 60);
            return true;
        }
        public void DamageBoss(int id, float amount)
        {
            if (Phase != MatchPhase.Hunt || !Players[id].Alive || amount <= 0) return;
            float effective = Math.Min(BossHealth, amount);
            BossHealth -= effective; Players[id].Contribution += effective;
            if (BossHealth > 0) return;
            // Stable player ID breaks ties; damage is capped to remaining HP (no overkill credit).
            Chooser = Players.OrderByDescending(p => p.Contribution).ThenBy(p => p.Id).First().Id;
            SetPhase(MatchPhase.Choice);
        }
        public bool ChooseRoute(int requester, bool demon)
        {
            if (Phase != MatchPhase.Choice || requester != Chooser) return false;
            foreach (var p in Players)
            {
                if (!p.Alive) Revive(p);
                p.Eliminated = false;
            }
            if (demon)
            {
                Demon = Chooser;
                var p = Players[Demon];
                p.MaxHealth = 800 + p.RewardCount * 70;
                p.Health = p.MaxHealth; p.Score = 0; p.RewardCount = 0; p.RevivalTokens = 0;
                // Sacrifice is a transition, never a kill or a consumed revival token.
                foreach (var other in Players.Where(x => x.Id != Demon)) Revive(other);
            }
            else Carrier = Chooser;
            Remaining = Rules.FinalSeconds;
            SetPhase(demon ? MatchPhase.Demon : MatchPhase.Escort);
            return true;
        }
        public void DamagePlayer(int id, float amount)
        {
            var p = Players[id];
            if (!Running || !p.Alive || amount <= 0) return;
            p.Health = Math.Max(0, p.Health - amount);
            if (p.Alive) return;
            if (Carrier == id) Carrier = -1;
            PlayerDied?.Invoke(id);
            if (Phase == MatchPhase.Demon && id == Demon)
            {
                p.Score = Rules.DemonLossScore;
                foreach (var survivor in Players.Where(x => x.Alive)) survivor.Score += Rules.SurvivalScore;
                Finish(HighestLiving(), "魔王被擊敗：存活最高分者獲勝"); return;
            }
            if (Phase == MatchPhase.Hunt) p.RespawnRemaining = Rules.RespawnSeconds;
            else if (p.RevivalTokens > 0)
            {
                p.RevivalTokens--; p.RespawnRemaining = Rules.RespawnSeconds;
            }
            else p.Eliminated = true;
            if (Phase == MatchPhase.Demon && Players.All(x => x.Id == Demon || x.Eliminated))
                Finish(Demon, "其他玩家全部永久淘汰");
        }
        public void DamageCrystal(int attacker, float amount)
        {
            if (Phase != MatchPhase.Demon || attacker != Demon || !Players[attacker].Alive || amount <= 0) return;
            CrystalHealth = Math.Max(0, CrystalHealth - amount);
            if (CrystalHealth <= 0) Finish(Demon, "王國水晶被摧毀");
        }
        public bool PickRelic(int id)
        {
            if (Phase != MatchPhase.Escort || Carrier >= 0 || !Players[id].Alive) return false;
            Carrier = id; return true;
        }
        public bool CompleteEscort(int id)
        {
            if (Phase != MatchPhase.Escort || Carrier != id || !Players[id].Alive) return false;
            Players[id].Score += Rules.EscortScore;
            foreach (var p in Players.Where(x => x.Alive)) p.Score += Rules.SurvivalScore;
            int winner = Players.OrderByDescending(x => x.Score).ThenBy(x => x.Id).First().Id;
            Finish(winner, "護送成功：依總分產生新王"); return true;
        }
        int HighestLiving() => Players.Where(p => p.Alive).OrderByDescending(p => p.Score).ThenBy(p => p.Id)
            .Select(p => p.Id).DefaultIfEmpty(-1).First();
        void Revive(Contestant p)
        {
            p.Eliminated = false; p.Health = p.MaxHealth; p.RespawnRemaining = 0;
            Respawned?.Invoke(p.Id);
        }
        void SetPhase(MatchPhase phase) { Phase = phase; PhaseChanged?.Invoke(phase); }
        void Finish(int winner, string reason) { Winner = winner; Result = reason; SetPhase(MatchPhase.Finished); }
    }
}

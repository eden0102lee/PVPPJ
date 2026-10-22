using System;
using GamePJ.Asymmetric;

static class RulesTests
{
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static AsymmetricMatch Final(bool demon)
    {
        var m = new AsymmetricMatch(4); m.DamageBoss(0, 2400); m.ChooseRoute(0, demon); return m;
    }
    static void Main()
    {
        var m = new AsymmetricMatch(4);
        m.Tick(719); Check(m.NewQuestsOpen, "quests available before 12m");
        m.Tick(1); Check(!m.NewQuestsOpen && m.QuestRewardsOpen, "12m closes acceptance only");
        m.Tick(480); Check(!m.CompleteQuest(0,false), "20m forbids quest rewards");
        m.Tick(600); Check(m.Phase == MatchPhase.Finished && m.Winner == -1, "30m hunt timeout loses for everyone");
        m = new AsymmetricMatch(4); m.Tick(1799); m.Players[1].RevivalTokens=2;
        m.DamagePlayer(1,999); m.DamageBoss(1,999); // Dead actors cannot contribute.
        Check(m.Players[1].Contribution==0,"dead players cannot contribute");
        m.DamageBoss(0,1500); m.DamageBoss(2,10000);
        Check(m.Chooser == 0 && m.Players[2].Contribution == 900,"effective damage wins over last hit / overkill");
        float remaining=m.Remaining, revive=m.Players[1].RespawnRemaining;
        m.Tick(1000); m.DamagePlayer(0,999);
        Check(m.Remaining==remaining && m.Players[1].RespawnRemaining==revive && m.Players[0].Alive,"choice freezes timers and damage");
        Check(!m.ChooseRoute(2,true),"only highest contributor can choose");
        Check(m.ChooseRoute(0,true) && m.Remaining==300,"late boss kill still grants full five minutes");
        Check(m.Players[1].Alive && m.Players[1].RevivalTokens==2,"sacrifice preserves revival rights");
        m.DamagePlayer(1,999); Check(!m.Players[1].Eliminated && m.Players[1].RevivalTokens==1,"sudden death accepts quest revival");
        m.Tick(8); Check(m.Players[1].Alive,"pending revival executes");
        m = Final(true); m.Tick(300); Check(m.Winner==0,"demon survives final timeout");
        m = Final(true); m.DamageCrystal(1,9999); Check(m.CrystalHealth==1800,"only demon damages kingdom crystal");
        m.DamageCrystal(0,9999); Check(m.Winner==0,"crystal destruction wins");
        m = Final(true); m.Players[1].RevivalTokens=1;
        m.DamagePlayer(1,999); m.DamagePlayer(2,999); m.DamagePlayer(3,999);
        Check(m.Phase==MatchPhase.Demon,"pending revival blocks all-eliminated victory");
        m.Tick(8); m.DamagePlayer(1,999); Check(m.Winner==0,"all permanent eliminations win");
        m = Final(true); m.Players[1].Score=200; m.Players[2].Score=500; m.DamagePlayer(2,999);
        m.DamagePlayer(0,9999); Check(m.Winner==1 && m.Players[0].Score==40,"demon loss uses fixed award and living winner");
        m = Final(false); m.Players[0].RevivalTokens=1; m.DamagePlayer(0,999);
        Check(m.Carrier==-1 && m.PickRelic(1),"dead carrier drops relic and another player can take over");
        Check(!m.CompleteEscort(0),"nonholder cannot finish escort");
        m.Players[2].Score=300; Check(m.CompleteEscort(1) && m.Winner==2,"escort completion grants points, not automatic victory");
        Check(m.Players[1].Score==180 && m.Players[0].Score==0,"only final carrier earns escort points; dead have no survival bonus");
        int score=m.Players[1].Score; Check(!m.CompleteEscort(1) && m.Players[1].Score==score,"settlement is idempotent");
        m = Final(false); m.Tick(300); Check(m.Winner==-1,"escort timeout loses for everyone");
        m = new AsymmetricMatch(4); m.CompleteQuest(0,true); m.CompleteQuest(0,false);
        m.DamageBoss(0,2400); m.ChooseRoute(0,true);
        Check(m.Players[0].MaxHealth==940 && m.Players[0].Score==0 && m.Players[0].RewardCount==0 && m.Players[0].RevivalTokens==0,"demon sacrifices rewards for strength");
        Console.WriteLine($"{passed} assertions passed.");
    }
}

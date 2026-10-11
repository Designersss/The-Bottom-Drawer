using System;
using BottomDrawer.Core;

internal static class Program
{
    private static int _passed;
    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new Exception("FAILED: " + name);
        _passed++;
    }

    private static void Main()
    {
        // Every more valuable base item must be heavier, regardless of rarity.
        var items = LootCatalog.Items;
        for (int i = 0; i < items.Length; i++)
        for (int j = 0; j < items.Length; j++)
            if (items[i].BasePrice > items[j].BasePrice)
                Assert(items[i].Weight > items[j].Weight, "strictly monotonic item mass");

        // Check strict monotonicity for the ENTIRE supported base-price domain,
        // not only for the ten handcrafted prototype definitions.
        for (int price = 2; price <= GameRules.MaximumBasePrice; price++)
            Assert(GameRules.WeightFromPrice(price) > GameRules.WeightFromPrice(price - 1),
                "all supported base prices produce strictly increasing weight");
        bool priceRejected = false;
        try { new ItemDefinition("bad", "bad", GameRules.MaximumBasePrice + 1, Rarity.Rare, true); }
        catch (ArgumentOutOfRangeException) { priceRejected = true; }
        Assert(priceRejected, "out-of-range item pricing rejected");

        Assert(GameRules.DragSpeed(4f, 5) > GameRules.DragSpeed(4f, 0), "strength increases speed");
        Assert(GameRules.DragSpeed(4f, 0) < GameRules.DragSpeed(0.5f, 0), "weight slows dragging");
        Assert(GameRules.DrainPerSecond(4f) > GameRules.DrainPerSecond(0.5f), "weight increases drain");
        Assert(Math.Abs(GameRules.DrainPerSecond(4f) - 7.5f) < .001f, "strength never changes drain rate");
        Assert(GameRules.MaxStamina(5) == 200, "stamina upgrade");

        var progress = new PlayerProgress();
        var session = new ExtractionSession(progress);
        session.Begin();
        Assert(session.Status == SessionStatus.Active && session.Stamina == 100, "session begins");
        Assert(session.Extract("item-1", items[0]), "extract once");
        Assert(!session.Extract("item-1", items[0]), "duplicate extraction blocked");
        Assert(session.DrainForDrag(items[0].Weight, 1f), "time-based drain");
        Assert(session.Stamina < 100, "spent stamina");
        Assert(session.Bank(), "safe bank");
        Assert(session.Sell("item-1") == items[0].BasePrice, "individual sale");
        Assert(session.Sell("item-1") == 0 && session.SellAll() == 0, "no double selling");
        Assert(progress.Coins == items[0].BasePrice, "banked earnings");
        session.Begin();
        session.Extract("item-2", items[9]);
        Assert(!session.DrainForDrag(items[9].Weight, 100f), "exhaustion ends run");
        Assert(session.Status == SessionStatus.Failed && session.Loot.Count == 0, "failed run forfeits loot");
        Assert(!session.Bank() && session.SellAll() == 0, "no banking after failure");
        Assert(progress.Coins == items[0].BasePrice, "previous coins survive failure");
        session.Begin();
        Assert(session.Stamina == 100, "new run restores stamina without waiting");

        var wealthy = new PlayerProgress(2000);
        var upgraded = new ExtractionSession(wealthy);
        Assert(upgraded.Buy(UpgradeType.Strength), "buy strength outside run");
        Assert(upgraded.Buy(UpgradeType.Stamina), "buy stamina outside run");
        upgraded.Begin();
        Assert(upgraded.Stamina == 120, "upgraded maximum stamina");
        Assert(!upgraded.Buy(UpgradeType.Luck), "cannot buy mid-run");
        upgraded.Extract("rare", items[8]);
        upgraded.Bank();
        int paid = upgraded.SellAll();
        Assert(paid == items[8].BasePrice, "sale without profit upgrade");
        Assert(upgraded.SellAll() == 0, "sell all idempotent");

        var profits = new PlayerProgress(100);
        Assert(profits.Buy(UpgradeType.Profit), "buy profit");
        Assert(GameRules.SalePrice(items[8], profits.Level(UpgradeType.Profit)) > items[8].BasePrice, "profit changes sale");
        Assert(items[8].Weight == GameRules.WeightFromPrice(items[8].BasePrice), "profit never changes weight");

        for (int index = 0; index < 500; index++)
        {
            ItemDefinition a = LootPicker.Pick(1234, index, 3, false);
            ItemDefinition b = LootPicker.Pick(1234, index, 3, false);
            Assert(a.Id == b.Id, "deterministic loot");
            Assert(a.Rarity != Rarity.Anomalous, "chapter-gated anomalies");
        }

        var restored = new ExtractionSession(new PlayerProgress());
        restored.Restore(SessionStatus.Active, 20f, new [] { new ExtractedItem("risk", items[1]) });
        Assert(restored.Status == SessionStatus.Failed && restored.Loot.Count == 0, "quitting cannot bank at-risk loot");
        var restoreBanked = new ExtractionSession(new PlayerProgress());
        restoreBanked.Restore(SessionStatus.Banked, 30f, new [] { new ExtractedItem("banked", items[2]) });
        Assert(restoreBanked.SellAll() == items[2].BasePrice, "banked loot survives reload");

        Console.WriteLine("PASS: " + _passed + " core assertions");
    }
}

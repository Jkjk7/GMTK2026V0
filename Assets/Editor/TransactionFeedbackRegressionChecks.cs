using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Real transaction paths must retain their economy semantics when UI feedback is added.</summary>
public static class TransactionFeedbackRegressionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try { Verify(); Debug.Log("[Transaction Feedback] PASS"); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        var random = UnityEngine.Random.state;
        try
        {
            DeniedPurchaseKeepsGoldOfferAndRandomness();
            FullHandAndLateInsertionFailureKeepOffer();
            SuccessfulReceiptIsImmediateAndDoesNotReplayOnSelection();
            BoardPurchaseKeepsItsSeparatePath();
            DeniedRefreshAndClosedSessionDoNotSpend();
        }
        finally { UnityEngine.Random.state = random; }
    }

    static void DeniedPurchaseKeepsGoldOfferAndRandomness()
    {
        using (var f = new Fixture())
        {
            f.SetGold(2);
            int insufficient = 0, spent = 0, changed = 0;
            f.Economy.OnGoldInsufficient += () => insufficient++;
            f.Economy.OnGoldSpent += _ => spent++;
            f.Economy.OnGoldChanged += _ => changed++;
            UnityEngine.Random.InitState(7181);
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(7181);
            Require(!f.Shop.TryPurchaseSlot(0), "Unaffordable purchase succeeded.");
            Require(f.Economy.CurrentGold == 2 && f.Slot.IsOccupied && f.Hand.OccupiedCount == 0,
                "Rejected purchase changed gold, consumed its offer, or inserted a card.");
            Require(insufficient == 1 && spent == 0 && changed == 0,
                "Rejection feedback duplicated or changed existing economy callbacks.");
            Require(UnityEngine.Random.value == expected, "Rejection consumed gameplay RNG.");
        }
    }

    static void FullHandAndLateInsertionFailureKeepOffer()
    {
        using (var f = new Fixture())
        {
            for (int i = 0; i < HandController.SlotCount; i++) f.Hand.GetSlot(i).SetCard(f.Card);
            int insufficient = 0;
            f.Economy.OnGoldInsufficient += () => insufficient++;
            Require(!f.Shop.TryPurchaseSlot(0), "Full hand accepted a purchase.");
            Require(f.Economy.CurrentGold == 50 && f.Slot.IsOccupied && insufficient == 0,
                "Full-hand rejection spent gold, lost its offer, or reported insufficient funds.");

            f.Hand.ClearHand();
            int gained = 0;
            f.Economy.OnGoldGained += _ => gained++;
            f.Economy.OnGoldSpent += _ =>
            {
                for (int i = 0; i < HandController.SlotCount; i++) f.Hand.GetSlot(i).SetCard(f.Card);
            };
            Require(!f.Shop.TryPurchaseSlot(0), "Purchase succeeded after its spend callback filled the hand.");
            Require(f.Economy.CurrentGold == 50 && f.Slot.IsOccupied && gained == 0,
                "Late insertion failure must silently refund exactly the spent amount and keep the offer.");
        }
    }

    static void SuccessfulReceiptIsImmediateAndDoesNotReplayOnSelection()
    {
        using (var f = new Fixture())
        {
            int spent = 0, changed = 0;
            f.Economy.OnGoldSpent += amount => spent += amount;
            f.Economy.OnGoldChanged += _ => changed++;
            UnityEngine.Random.InitState(411);
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(411);
            Require(f.Shop.TryPurchaseSlot(0), "Affordable purchase failed.");
            HandSlot received = f.Hand.GetSlot(0);
            Require(f.Economy.CurrentGold == 40 && spent == 10 && changed == 1
                && !f.Slot.IsOccupied && f.Hand.OccupiedCount == 1,
                "Purchase must synchronously spend once, clear its offer, and insert one card.");
            Require(received.CardData.InvestedGold == 10 && received.CardData.Level == 1
                && received.CardData.Type == f.Card.Type && received.CardData.ScrapRefund == 3,
                "Receipt feedback changed purchased card data or its refund.");
            Require(UnityEngine.Random.value == expected, "Receipt feedback consumed gameplay RNG.");

            Component feedback = FeedbackOn(received);
            Require(GetField<object>(feedback, "_kind").ToString() == "Receive",
                "Successful insertion must start a receipt cue immediately.");
            feedback.GetType().GetMethod("Tick", Private).Invoke(feedback, new object[] { 0.1f });
            float age = GetField<float>(feedback, "_age");
            feedback.GetType().GetMethod("OnPointerEnter").Invoke(feedback, new object[] { null });
            received.SetSelected(true);
            received.SetSelected(false);
            f.Shop.RefreshAffordability();
            Require(GetField<float>(feedback, "_age") == age,
                "Hover, selection and affordability redraws must not replay receipt feedback.");
            Require(f.Hand.TryAddCard(f.Card), "Board-return or reward-style hand insertion failed.");
            Require(GetField<object>(FeedbackOn(f.Hand.GetSlot(1)), "_kind").ToString() == "Receive",
                "A returned or awarded card needs the same receipt cue as a purchase.");
        }
    }

    static void BoardPurchaseKeepsItsSeparatePath()
    {
        using (var f = new Fixture())
        {
            Require(f.Shop.TryPurchaseForBoard(0, out var card, out int paid), "Board purchase failed.");
            Require(paid == 10 && card.InvestedGold == 10 && f.Economy.CurrentGold == 40
                && f.Hand.OccupiedCount == 0 && !f.Slot.IsOccupied,
                "Board purchase must spend once without inserting into the hand.");
            f.Slot.SetOffer(f.Card, 100);
            Require(!f.Shop.TryPurchaseForBoard(0, out card, out paid) && paid == 0
                && f.Slot.IsOccupied && f.Economy.CurrentGold == 40,
                "Failed board purchase lost its offer or leaked a paid amount.");
        }
    }

    static void DeniedRefreshAndClosedSessionDoNotSpend()
    {
        using (var f = new Fixture())
        {
            f.SetGold(0);
            int insufficient = 0;
            f.Economy.OnGoldInsufficient += () => insufficient++;
            f.Shop.TryRefreshPaid();
            Require(insufficient == 1 && f.Economy.CurrentGold == 0 && f.Slot.Price == 10,
                "Denied refresh must preserve the existing offer and insufficient callback.");
            typeof(GameSession).GetField("_state", Private).SetValue(f.Session, GameSessionState.Defeat);
            Require(!f.Shop.TryPurchaseSlot(0), "Inactive session accepted a shop purchase.");
            f.Shop.TryRefreshPaid();
            Require(insufficient == 1 && f.Slot.Price == 10,
                "An interaction gate must return before transaction feedback or spending.");
        }
    }

    static Component FeedbackOn(Component target)
    {
        Type type = typeof(HandSlot).Assembly.GetType("UiFeedback");
        Require(type != null, "Receipt feedback component is missing.");
        Component feedback = target.GetComponent(type);
        Require(feedback != null, "Receipt feedback did not attach to the received card.");
        return feedback;
    }

    static T GetField<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, Private).GetValue(instance);

    sealed class Fixture : IDisposable
    {
        readonly GameObject _root = new GameObject("TransactionRegression");
        readonly Economy _priorEconomy = Economy.Instance;
        readonly GameSession _priorSession = GameSession.Instance;
        readonly RunModifiers _priorModifiers = RunModifiers.Instance;
        public readonly Economy Economy;
        public readonly GameSession Session;
        public readonly HandController Hand;
        public readonly ShopController Shop;
        public readonly ShopSlot Slot;
        public readonly ModuleCardData Card;

        public Fixture()
        {
            SetSingleton(typeof(RunModifiers), null);
            Economy = _root.AddComponent<Economy>();
            Session = _root.AddComponent<GameSession>();
            SetSingleton(typeof(Economy), Economy);
            SetSingleton(typeof(GameSession), Session);
            SetGold(50);
            Hand = _root.AddComponent<HandController>();
            var slots = new HandSlot[HandController.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                var go = new GameObject("Hand" + i, typeof(RectTransform));
                go.transform.SetParent(_root.transform, false);
                slots[i] = go.AddComponent<HandSlot>();
                slots[i].Setup(Hand, i, null);
            }
            Hand.BindSlots(slots);
            Shop = _root.AddComponent<ShopController>();
            var offer = new GameObject("Offer", typeof(RectTransform));
            offer.transform.SetParent(_root.transform, false);
            Slot = offer.AddComponent<ShopSlot>();
            Slot.Setup(Shop, 0, null);
            Shop.Initialize(Hand, new[] { Slot }, Session);
            Card = ModuleCardData.Create(ModuleType.FlameAmp, 1, 0);
            Slot.SetOffer(Card, 10);
        }

        public void SetGold(int amount) => typeof(Economy).GetField("currentGold", Private).SetValue(Economy, amount);
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            SetSingleton(typeof(Economy), _priorEconomy);
            SetSingleton(typeof(GameSession), _priorSession);
            SetSingleton(typeof(RunModifiers), _priorModifiers);
        }
        static void SetSingleton(Type type, object value) =>
            type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
    }

    static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real UI lifecycle checks; no animation callback may control a reward or gameplay clock.</summary>
public static class RewardFeedbackRegressionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try { Verify(); Debug.Log("[Reward Feedback] PASS"); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        bool motion = PresentationSettings.MotionEnabled;
        var quality = PresentationSettings.Quality;
        var random = UnityEngine.Random.state;
        float timeScale = Time.timeScale;
        try
        {
            PresentationSettings.SetMotion(true, false);
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Cinematic, false);
            DraftLifecycleAndPausedClock();
            ModalLifecycle();
            ConfirmCallbacksStayImmediate();
            ResultReturnIsOneShot();
        }
        finally
        {
            Time.timeScale = timeScale;
            PresentationSettings.SetMotion(motion, false);
            PresentationSettings.SetQuality(quality, false);
            UnityEngine.Random.state = random;
        }
    }

    static void DraftLifecycleAndPausedClock()
    {
        GameObject root = new GameObject("RewardLifecycle", typeof(RectTransform), typeof(CanvasGroup));
        GameObject economyOwner = null;
        try
        {
            Economy economy = Economy.Instance;
            if (economy == null)
            {
                economyOwner = new GameObject("RewardEconomy");
                economy = economyOwner.AddComponent<Economy>();
            }
            int gold = economy.CurrentGold;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(920, 420);
            rect.anchoredPosition3D = new Vector3(15, 26, 0);
            rect.localScale = new Vector3(0.9f, 1.1f, 1);
            Vector3 rootPosition = rect.anchoredPosition3D, rootScale = rect.localScale;
            var buttons = new[] { Button(root.transform, "Choice0"), Button(root.transform, "Choice1"), Button(root.transform, "Choice2") };
            var view = root.AddComponent<DraftChoiceView>();
            var group = root.GetComponent<CanvasGroup>();
            view.Bind(group, null, buttons, null);
            int calls = 0, chosen = -1;
            var labels = new[] { "A", "B", "C" };
            UnityEngine.Random.InitState(1179);
            float expectedRandom = UnityEngine.Random.value;
            UnityEngine.Random.InitState(1179);
            Time.timeScale = 0;
            view.ShowCustom("Choose", labels, index => { calls++; chosen = index; });
            Type revealType = typeof(DraftChoiceView).Assembly.GetType("RewardReveal");
            Require(revealType != null, "Draft choices need a reusable RewardReveal presentation.");
            Component reveal = root.GetComponent(revealType);
            Require(reveal != null, "Showing a draft must attach reward presentation.");
            Transform runes = root.transform.Find("RewardRunes");
            Require(runes != null && CountActiveRunes(runes) == 12, "Cinematic rewards must respect the 12-mote ceiling.");
            foreach (Graphic decoration in runes.GetComponentsInChildren<Graphic>(true))
                Require(!decoration.raycastTarget, "Reward decoration may never intercept a choice click.");
            int childCount = root.GetComponentsInChildren<Transform>(true).Length;
            RectTransform cardMotion = buttons[0].transform.parent as RectTransform;
            Require(cardMotion != rect, "Reward motion needs its own wrapper separate from pointer feedback.");
            Vector3 initialScale = cardMotion.localScale;
            Tick(reveal, 0.18f);
            Require(cardMotion.localScale != initialScale, "Reward animation must advance with an unscaled tick while paused.");
            Require(Time.timeScale == 0 && group.interactable && group.blocksRaycasts,
                "Reveal must preserve pause state and keep rewards immediately selectable.");
            buttons[1].onClick.Invoke();
            buttons[1].onClick.Invoke();
            Require(calls == 1 && chosen == 1, "Selection must invoke the original choice immediately and exactly once.");
            Require(group.alpha == 0 && !group.blocksRaycasts && !group.interactable,
                "Selecting during reveal must immediately remove the hidden modal's raycast block.");
            Require(cardMotion.localScale == Vector3.one, "Hide must restore the exact authored card pose.");
            for (int i = 0; i < 6; i++)
            {
                view.ShowCustom("Reopen", labels, _ => calls++);
                Tick(reveal, 0.07f);
                view.Hide();
                Tick(reveal, 1f);
                Require(cardMotion.localScale == Vector3.one && group.alpha == 0 && !group.blocksRaycasts,
                    "An interrupted reveal must not resurrect a hidden draft or compound its scale.");
            }
            Require(root.GetComponentsInChildren<Transform>(true).Length == childCount,
                "Repeated draft opens must reuse a bounded decoration hierarchy.");
            PresentationSettings.SetMotion(false, false);
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Reduced, false);
            view.ShowCustom("Reduced", labels, _ => calls++);
            Require(CountActiveRunes(runes) == 3, "Reduced effect quality must lower the reward mote budget.");
            Vector3 cardPosition = cardMotion.anchoredPosition3D;
            Tick(reveal, 0.16f);
            Require(cardMotion.localScale == Vector3.one && cardMotion.anchoredPosition3D == cardPosition
                && cardMotion.localRotation == Quaternion.identity, "Reduced motion may not move, rotate or scale reward cards.");
            Require(rect.anchoredPosition3D == rootPosition && rect.localScale == rootScale,
                "Reward reveal must preserve the modal root and scrim geometry.");
            view.Hide();
            PresentationSettings.SetMotion(true, false);
            var options = new List<DraftChoiceView.Option> { new DraftChoiceView.Option { AddType = (ModuleType)0 } };
            ModuleType chosenType = (ModuleType)(-1);
            view.Show(options, option => { calls++; chosenType = option.AddType; });
            int beforePick = calls;
            buttons[0].onClick.Invoke();
            buttons[0].onClick.Invoke();
            Require(calls == beforePick + 1 && chosenType == options[0].AddType,
                "Module rewards must retain their exact option and one-shot callback.");
            Require(UnityEngine.Random.value == expectedRandom, "Reward presentation must not consume gameplay RNG.");
            Require(economy.CurrentGold == gold, "Reward presentation must not modify resources.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (economyOwner != null) UnityEngine.Object.DestroyImmediate(economyOwner);
        }
    }

    static void ModalLifecycle()
    {
        GameObject root = new GameObject("ModalLifecycle", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            var rect = root.GetComponent<RectTransform>();
            var group = root.GetComponent<CanvasGroup>();
            rect.anchoredPosition3D = new Vector3(31, 12, 3);
            rect.localScale = new Vector3(0.87f, 1.13f, 1);
            rect.localRotation = Quaternion.Euler(0, 0, 4);
            Vector3 position = rect.anchoredPosition3D, scale = rect.localScale;
            Quaternion rotation = rect.localRotation;
            group.alpha = 0.62f; group.interactable = false; group.blocksRaycasts = false;
            Type type = typeof(DraftChoiceView).Assembly.GetType("ModalReveal");
            Require(type != null, "Modal panels need a reusable presentation-only reveal.");
            MethodInfo play = type.GetMethod("Play", BindingFlags.Public | BindingFlags.Static);
            MethodInfo cancel = type.GetMethod("Cancel", BindingFlags.Public | BindingFlags.Static);
            Component reveal = (Component)play.Invoke(null, new object[] { rect, true });
            Tick(reveal, 0.1f);
            cancel.Invoke(null, new object[] { rect });
            Require(rect.anchoredPosition3D == position && rect.localScale == scale && rect.localRotation == rotation,
                "Cancelled modals must restore all exact authored transforms.");
            int children = root.GetComponentsInChildren<Transform>(true).Length;
            for (int i = 0; i < 4; i++)
            {
                play.Invoke(null, new object[] { rect, true });
                Tick(reveal, 0.05f);
                play.Invoke(null, new object[] { rect, false });
                Tick(reveal, 2f);
            }
            Require(rect.anchoredPosition3D == position && rect.localScale == scale && rect.localRotation == rotation,
                "Interrupted/reopened modal spring must never accumulate transform changes.");
            Require(root.GetComponentsInChildren<Transform>(true).Length == children,
                "Modal decoration must be cached across repeat opens.");
            Require(group.alpha == 0.62f && !group.interactable && !group.blocksRaycasts,
                "Modal animation must leave view-owned alpha and input state untouched.");
            PresentationSettings.SetMotion(false, false);
            play.Invoke(null, new object[] { rect, true });
            Tick(reveal, 0.07f);
            Require(rect.anchoredPosition3D == position && rect.localScale == scale && rect.localRotation == rotation,
                "Reduced motion must suppress all modal transforms.");
            root.SetActive(false);
            Require(rect.anchoredPosition3D == position && rect.localScale == scale,
                "Disabling a modal mid-reveal must restore its rest pose.");
        }
        finally { PresentationSettings.SetMotion(true, false); UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ConfirmCallbacksStayImmediate()
    {
        GameObject root = new GameObject("ConfirmLifecycle", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            var confirm = Button(root.transform, "Confirm");
            var cancel = Button(root.transform, "Cancel");
            var view = root.AddComponent<ConfirmPromptView>();
            view.Bind(root.GetComponent<CanvasGroup>(), null, null, null, confirm, cancel, null);
            int confirmed = 0, cancelled = 0;
            view.Show("Confirm", "Body", "Yes", true, null, () => confirmed++, () => cancelled++);
            confirm.onClick.Invoke(); confirm.onClick.Invoke();
            Require(confirmed == 1 && cancelled == 0 && !view.IsOpen, "Confirm must close and invoke exactly once without an animation delay.");
            view.Show("Confirm", "Body", "Yes", false, null, () => confirmed++, () => cancelled++);
            confirm.onClick.Invoke();
            Require(confirmed == 1 && view.IsOpen, "A disabled confirmation may not invoke its action.");
            cancel.onClick.Invoke(); cancel.onClick.Invoke();
            Require(cancelled == 1 && !view.IsOpen, "Cancel must close and invoke exactly once.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ResultReturnIsOneShot()
    {
        GameObject root = new GameObject("ResultLifecycle", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            var menu = Button(root.transform, "Menu");
            var view = root.AddComponent<ResultOverlayView>();
            view.Bind(root.GetComponent<CanvasGroup>(), null, menu);
            int returns = 0;
            view.SetReturnToMenuHandler(() => returns++);
            PresentationSettings.SetMotion(false, false);
            view.Show("Complete", Color.white);
            menu.onClick.Invoke(); menu.onClick.Invoke();
            Require(returns == 1, "Result return must be one-shot even during an opening animation.");
            view.HideImmediate();
            Require(root.GetComponent<CanvasGroup>().alpha == 0 && !root.activeSelf,
                "Result hide must stop any ongoing fade and remain hidden.");
        }
        finally { PresentationSettings.SetMotion(true, false); UnityEngine.Object.DestroyImmediate(root); }
    }

    static Button Button(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        child.transform.SetParent(parent, false);
        child.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 250);
        return child.GetComponent<Button>();
    }

    static int CountActiveRunes(Transform root)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
            if (root.GetChild(i).name.StartsWith("Rune") && root.GetChild(i).gameObject.activeSelf) count++;
        return count;
    }

    static void Tick(Component component, float seconds)
    {
        MethodInfo method = component.GetType().GetMethod("Tick", Private);
        Require(method != null, "Presentation needs an unscaled deterministic Tick for lifecycle verification.");
        method.Invoke(component, new object[] { seconds });
    }

    static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}

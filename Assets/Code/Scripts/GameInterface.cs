using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Desconecta
{
    /// <summary>Builds a scalable interface with explicit keyboard focus and audio descriptions.</summary>
    public sealed class GameInterface : MonoBehaviour
    {
        public Font font;
        public Sprite illustration;
        private GameObject content;
        private RectTransform canvas;
        private Text scoreText, timeText, objectiveText, focusText, feedbackText, captionText;
        private bool optionsVisible;
        private Button firstButton;
        private readonly Color navy = new Color32(17, 35, 48, 255);
        private readonly Color cream = new Color32(250, 247, 236, 255);
        private readonly Color green = new Color32(29, 86, 67, 255);
        private readonly Color gold = new Color32(255, 206, 92, 255);

        private int Body => GameSession.Instance != null && GameSession.Instance.LargeText ? 34 : 28;

        private void Awake()
        {
            var root = new GameObject("AccessibleInterface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<RectTransform>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("MenuEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public void RefreshScreen()
        {
            if (canvas == null || GameSession.Instance == null) return;
            if (content != null) { content.SetActive(false); Destroy(content); }
            content = new GameObject("CurrentScreen", typeof(RectTransform));
            content.transform.SetParent(canvas, false);
            var stretch = content.GetComponent<RectTransform>();
            stretch.anchorMin = Vector2.zero; stretch.anchorMax = Vector2.one; stretch.offsetMin = stretch.offsetMax = Vector2.zero;
            firstButton = null;
            var session = GameSession.Instance;
            if (session.Screen == SessionScreen.Playing) { optionsVisible = false; BuildHud(stretch); return; }
            var backdrop = Panel(stretch, "Backdrop", new Color(0.025f,.06f,.08f,.9f));
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var page = Panel(stretch, "MenuCard", cream);
            page.anchorMin = page.anchorMax = new Vector2(.5f,.5f);
            page.sizeDelta = new Vector2(1340, 860);
            page.anchoredPosition = Vector2.zero;
            Label(page, "DESCONECTA  /  ECOHOGAR", 60, 32, 1200, 38, 23, green);
            if (optionsVisible || session.Screen == SessionScreen.Paused) BuildOptions(page);
            else if (session.Screen == SessionScreen.Home) BuildHome(page);
            else if (session.Screen == SessionScreen.Decision) BuildDecision(page);
            else BuildResults(page);
            if (firstButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        private void BuildHome(RectTransform page)
        {
            var session = GameSession.Instance;
            Label(page, "Pequeñas acciones.\nUn hogar mejor.", 60, 100, 740, 150, 58, navy, true);
            Label(page, "Explora la casa y resuelve 7 situaciones de agua, energía y gas. Tienes 10 minutos; también puedes elegir jugar sin límite.", 60, 280, 700, 160, Body, navy);
            Label(page, "MOVERSE  Flechas o WASD\nMIRAR  Mouse · o Q/E y R/F\nREVISAR  Clic, Enter o Espacio\nDECIDIR  1 / 2 · PAUSA  Esc · PISTA  H", 60, 465, 720, 175, Body, navy);
            ActionButton(page, "Comenzar recorrido", 60, 680, 710, 76, session.Begin, true);
            ActionButton(page, "Accesibilidad y sonido", 60, 772, 710, 60, () => { optionsVisible = true; RefreshScreen(); });
            var imagePanel = Panel(page, "Illustration", new Color32(229,235,215,255));
            Place(imagePanel, 825, 100, 450, 450);
            if (illustration != null) { var img = imagePanel.GetComponent<Image>(); img.sprite = illustration; img.preserveAspect = true; img.color = Color.white; }
            Label(page, "AGUA · ENERGÍA · CUIDADO", 825, 575, 450, 65, 25, green, true);
            Label(page, "+100 por acierto\n−25 por una elección incorrecta\nPuedes volver a intentarlo.\n\nCon el gas, pide ayuda a un adulto.", 825, 660, 450, 165, 26, navy);
        }

        private void BuildDecision(RectTransform page)
        {
            var session = GameSession.Instance;
            var situation = session.Selected;
            Label(page, situation.number + "/7  ·  " + situation.room.ToUpperInvariant(), 60, 100, 1200, 45, 28, green, true);
            Label(page, situation.shortLabel, 60, 160, 1200, 80, 52, navy, true);
            Label(page, situation.question, 60, 265, 1210, 145, Body + 2, navy);
            Label(page, situation.soundDescription, 60, 410, 1210, 60, 26, green);
            ActionButton(page, "1   " + situation.optionA, 60, 485, 1210, 86, () => session.Answer(0), true);
            ActionButton(page, "2   " + situation.optionB, 60, 590, 1210, 86, () => session.Answer(1));
            Label(page, session.Feedback, 60, 690, 1210, 85, 27, navy);
            ActionButton(page, "Volver a explorar  ·  Esc", 60, 780, 1210, 62, session.Resume);
        }

        private void BuildOptions(RectTransform page)
        {
            var session = GameSession.Instance;
            Label(page, "A tu ritmo", 60, 100, 1200, 80, 54, navy, true);
            Label(page, "El tiempo se detiene aquí. Usa Tab o flechas para elegir y Enter para activar.", 60, 195, 1200, 70, Body, navy);
            OptionButton(page, "Texto grande", session.LargeText, 60, 290, v => session.SetLargeText(v));
            OptionButton(page, "Movimiento reducido", session.ReducedMotion, 690, 290, v => session.SetReducedMotion(v));
            OptionButton(page, "Sin límite de tiempo", session.UnlimitedTime, 60, 390, v => session.SetUnlimited(v));
            OptionButton(page, "Silenciar todo", session.Muted, 690, 390, v => session.SetMuted(v));
            SliderRow(page, "Música", session.MusicVolume, 60, 500, session.SetMusic);
            SliderRow(page, "Efectos", session.EffectsVolume, 690, 500, session.SetEffects);
            SliderRow(page, "Sensibilidad del mouse", session.Sensitivity, 60, 610, session.SetSensitivity);
            Label(page, "Los sonidos siempre tienen una descripción.\nLas situaciones se distinguen por número y texto.", 690, 607, 570, 90, 27, navy);
            ActionButton(page, session.Screen == SessionScreen.Home ? "Volver al inicio" : "Continuar recorrido", 60, 745, 580, 70, () => { optionsVisible = false; if (session.Screen == SessionScreen.Home) RefreshScreen(); else session.Resume(); }, true);
            ActionButton(page, "Volver al menú", 690, 745, 580, 70, () => { optionsVisible = false; session.Home(); });
        }

        private void BuildResults(RectTransform page)
        {
            var session = GameSession.Instance;
            bool won = session.Progress.Won;
            Label(page, won ? "¡Un hogar mejor!" : "Cada intento enseña", 60, 110, 1200, 110, 60, navy, true);
            Label(page, session.Progress.Completed + " / 7 situaciones resueltas     ·     " + session.Progress.Score + " puntos", 60, 255, 1200, 75, 42, green, true);
            Label(page, won ? "Cerraste grifos, apagaste lo que no se utilizaba y elegiste pedir ayuda con el gas." : "El tiempo terminó. Puedes volver a empezar o activar el modo sin límite desde Accesibilidad.", 60, 370, 1200, 145, Body + 3, navy);
            Label(page, "Lleva lo aprendido a casa:\n• Cierra el agua cuando no la necesitas.\n• Apaga luces y aparatos que no están en uso.\n• Pide a un adulto que revise la estufa.", 60, 530, 1200, 170, Body, navy);
            ActionButton(page, "Jugar otra vez", 60, 750, 580, 70, session.Begin, true);
            ActionButton(page, "Ir al menú", 690, 750, 580, 70, session.Home);
        }

        private void BuildHud(RectTransform root)
        {
            var top = Panel(root, "StatusBar", navy);
            top.anchorMin = new Vector2(0,1); top.anchorMax = Vector2.one;
            top.pivot = new Vector2(.5f,1); top.sizeDelta = new Vector2(0,105); top.anchoredPosition = Vector2.zero;
            Label(top, "DESCONECTA", 32, 20, 390, 35, 30, cream, true);
            scoreText = Label(top, "", 500, 20, 550, 64, Body, cream);
            timeText = Label(top, "", 1120, 20, 440, 64, Body, gold);
            var list = Panel(root, "ObjectivePanel", new Color32(17,35,48,242));
            Place(list, 24, 132, 445, 375);
            Label(list, "TU RECORRIDO", 22, 15, 400, 40, 25, gold, true);
            objectiveText = Label(list, "", 22, 70, 400, 290, 25, cream);
            var bottom = Panel(root, "FeedbackPanel", new Color32(17,35,48,245));
            bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1,0); bottom.pivot = new Vector2(.5f,0);
            bottom.sizeDelta = new Vector2(0,200); bottom.anchoredPosition = Vector2.zero;
            feedbackText = Label(bottom, "", 30, 15, 1510, 70, Body, cream);
            captionText = Label(bottom, "", 30, 85, 1510, 50, 25, gold);
            Label(bottom, "Flechas/WASD: moverse   ·   Q/E: girar   ·   R/F: mirar   ·   Esc: pausa   ·   H: pista", 30, 145, 1510, 40, 23, cream);
            var cross = Label(root, "+", 0, 0, 52, 52, 44, Color.white, true);
            var crossRect = cross.rectTransform;
            crossRect.anchorMin = crossRect.anchorMax = new Vector2(.5f,.5f); crossRect.pivot = new Vector2(.5f,.5f); crossRect.anchoredPosition = Vector2.zero;
            cross.alignment = TextAnchor.MiddleCenter;
            cross.gameObject.AddComponent<Outline>().effectColor = Color.black;
            focusText = Label(root, "", 0, 0, 850, 90, Body + 2, Color.white, true);
            focusText.rectTransform.anchorMin = focusText.rectTransform.anchorMax = new Vector2(.5f,.5f);
            focusText.rectTransform.pivot = new Vector2(.5f,1); focusText.rectTransform.anchoredPosition = new Vector2(0,-65);
            focusText.alignment = TextAnchor.MiddleCenter;
            focusText.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        private void Update()
        {
            var session = GameSession.Instance;
            if (session != null && session.Screen != SessionScreen.Playing && Keyboard.current != null && GameInput.Pressed(Keyboard.current.tabKey, KeyCode.Tab) && EventSystem.current != null)
            {
                var current = EventSystem.current.currentSelectedGameObject;
                var selected = current != null ? current.GetComponent<Selectable>() : null;
                var next = selected != null ? (GameInput.Held(Keyboard.current.shiftKey, KeyCode.LeftShift) ? selected.FindSelectableOnUp() : selected.FindSelectableOnDown()) : firstButton;
                if (next == null) next = firstButton;
                if (next != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            }
            if (session == null || session.Screen != SessionScreen.Playing || scoreText == null) return;
            scoreText.text = session.Progress.Completed + "/7 resueltas  ·  " + session.Progress.Score + " puntos";
            int seconds = Mathf.CeilToInt(session.Progress.RemainingSeconds);
            timeText.text = session.Progress.Unlimited ? "SIN LÍMITE" : "TIEMPO  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00");
            string list = "";
            foreach (var situation in session.situations) list += (situation.IsResolved ? "✓ " : "○ ") + situation.number + "  " + situation.shortLabel + "\n";
            objectiveText.text = list;
            var focus = session.explorer.Focused;
            focusText.text = focus == null ? "" : focus.shortLabel + "\nClic · Enter · Espacio para revisar";
            feedbackText.text = session.Feedback;
            captionText.text = Time.unscaledTime < session.CaptionUntil ? session.Caption : focus != null ? focus.soundDescription : "[Música ambiental suave] · Las alertas también aparecen como texto.";
        }

        private RectTransform Panel(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return obj.GetComponent<RectTransform>();
        }

        private void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height);
        }

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, bool bold = false)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent,false);
            var text = obj.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            Place(text.rectTransform,x,y,width,height); return text;
        }

        private Button ActionButton(RectTransform page, string title, float x, float y, float width, float height, Action action, bool primary = false)
        {
            var rect = Panel(page,"Button_" + title, primary ? green : navy); Place(rect,x,y,width,height);
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1,.9f,.6f); colors.selectedColor = new Color(1,.9f,.6f); colors.pressedColor = new Color(.65f,.85f,.7f); button.colors = colors;
            var label = Label(rect,title,22,7,width-44,height-14,Body,cream,true); label.alignment = TextAnchor.MiddleLeft;
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = gold; outline.effectDistance = new Vector2(5,-5);
            rect.gameObject.AddComponent<VisibleMenuFocus>();
            button.onClick.AddListener(() => action()); if (firstButton == null) firstButton = button;
            return button;
        }

        private void OptionButton(RectTransform page, string name, bool value, float x, float y, Action<bool> set)
        {
            ActionButton(page,name + ": " + (value ? "SÍ" : "NO"),x,y,580,74,() => { set(!value); RefreshScreen(); });
        }

        private void SliderRow(RectTransform page, string name, float value, float x, float y, Action<float> set)
        {
            var caption = Label(page,name + "  " + Mathf.RoundToInt(value*100) + "%",x,y,570,38,Body,navy,true);
            var track = Panel(page,"Slider_" + name,navy); Place(track,x,y+48,570,27); track.GetComponent<Image>().raycastTarget=true;
            var slider = track.gameObject.AddComponent<Slider>(); slider.minValue=0; slider.maxValue=1;
            var handle = Panel(track,"Handle",green); handle.sizeDelta = new Vector2(38,50); handle.GetComponent<Image>().raycastTarget=true;
            slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>(); slider.value=value;
            slider.onValueChanged.AddListener(v => set(v));
            // Screen text is updated without rebuilding the focused slider.
            slider.onValueChanged.AddListener(v => caption.text=name + "  " + Mathf.RoundToInt(v*100)+"%");
            var outline = track.gameObject.AddComponent<Outline>(); outline.effectColor=gold; outline.effectDistance=new Vector2(5,-5); track.gameObject.AddComponent<VisibleMenuFocus>();
        }
    }
}

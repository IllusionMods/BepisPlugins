/*
 * 
// Popup list created by Eric Haines
// ComboBox Extended by Hyungseok Seo.(Jerry) sdragoon@nate.com
// this oop version of ComboBox is refactored by zhujiangbo jumbozhu@gmail.com
// Modified by MarC0 / ManlyMarco
// 
// -----------------------------------------------
// This code working like ComboBox Control.
// I just changed some part of code, 
// because I want to seperate ComboBox button and List.
// ( You can see the result of this code from Description's last picture )
// -----------------------------------------------
//
// === usage ======================================
using UnityEngine;
using System.Collections;
 
public class ComboBoxTest : MonoBehaviour
{
	GUIContent[] comboBoxList;
	private ComboBox comboBoxControl;// = new ComboBox();
	private GUIStyle listStyle = new GUIStyle();
 
	private void Start()
	{
		comboBoxList = new GUIContent[5];
		comboBoxList[0] = new GUIContent("Thing 1");
		comboBoxList[1] = new GUIContent("Thing 2");
		comboBoxList[2] = new GUIContent("Thing 3");
		comboBoxList[3] = new GUIContent("Thing 4");
		comboBoxList[4] = new GUIContent("Thing 5");
 
		listStyle.normal.textColor = Color.white; 
		listStyle.onHover.background =
		listStyle.hover.background = new Texture2D(2, 2);
		listStyle.padding.left =
		listStyle.padding.right =
		listStyle.padding.top =
		listStyle.padding.bottom = 4;
 
		comboBoxControl = new ComboBox(new Rect(50, 100, 100, 20), comboBoxList[0], comboBoxList, "button", "box", listStyle);
	}
 
	private void OnGUI () 
	{
		int selectedItemIndex = comboBoxControl.Show();
		GUI.Label( new Rect(50, 70, 400, 21), "dfdsfYou picked " + comboBoxList[selectedItemIndex].text + "!" );
	}
}
 
*/

using System;
using UnityEngine;

namespace ConfigurationManager.Utilities
{
    public class ComboBox
    {
        private static bool forceToUnShow;
        private static int useControlID = -1;
        private readonly string boxStyle;
        private readonly string buttonStyle;
        private bool isClickedComboButton;
        private readonly GUIContent[] listContent;
        private readonly GUIStyle listStyle;
        private readonly int _windowYmax;

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle, float windowYmax)
        {
            Rect = rect;
            ButtonContent = buttonContent;
            this.listContent = listContent;
            buttonStyle = "button";
            boxStyle = "box";
            this.listStyle = listStyle;
            _windowYmax = (int) windowYmax;
        }

        public ComboBox(Rect rect, GUIContent buttonContent, GUIContent[] listContent, string buttonStyle,
            string boxStyle, GUIStyle listStyle)
        {
            Rect = rect;
            ButtonContent = buttonContent;
            this.listContent = listContent;
            this.buttonStyle = buttonStyle;
            this.boxStyle = boxStyle;
            this.listStyle = listStyle;
        }

        public Rect Rect { get; set; }

        public GUIContent ButtonContent { get; set; }

        public void Show(Action<int> onItemSelected)
        {
            if (forceToUnShow)
            {
                forceToUnShow = false;
                isClickedComboButton = false;
            }

            var done = false;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            Vector2 currentMousePosition = Vector2.zero;
            if (Event.current.GetTypeForControl(controlID) == EventType.mouseUp)
            {
                if (isClickedComboButton)
                {
                    done = true;
                    currentMousePosition = Event.current.mousePosition;
                }
            }

            if (GUI.Button(Rect, ButtonContent, buttonStyle))
            {
                if (useControlID == -1)
                {
                    useControlID = controlID;
                    isClickedComboButton = false;
                }

                if (useControlID != controlID)
                {
                    forceToUnShow = true;
                    useControlID = controlID;
                }
                isClickedComboButton = true;
            }

            if (isClickedComboButton)
            {
                GUI.enabled = false;
                GUI.color = new Color(1, 1, 1, 2);

                var location = GUIUtility.GUIToScreenPoint(new Vector2(Rect.x, Rect.y + listStyle.CalcHeight(listContent[0], 1.0f)));
                var size = new Vector2(Rect.width, listStyle.CalcHeight(listContent[0], 1.0f) * listContent.Length);

                var innerRect = new Rect(Vector2.zero, size);

                var outerRectScreen = new Rect(location, size);
                if (outerRectScreen.yMax > _windowYmax)
                {
                    outerRectScreen.height = _windowYmax - outerRectScreen.y;
                    outerRectScreen.width += 20;
                }
                
                if (currentMousePosition != Vector2.zero && outerRectScreen.Contains(GUIUtility.GUIToScreenPoint(currentMousePosition)))
                    done = false;
                
                CurrentDropdownDrawer = () =>
                {
                    GUI.enabled = true;

                    var outerRectLocal = GUIUtility.ScreenToGUIRect(outerRectScreen);
                    
                    GUI.Box(outerRectLocal, GUIContent.none, 
                        new GUIStyle { normal = new GUIStyleState { background = ConfigurationManager.WindowBackground } });

                    _scrollPosition = GUI.BeginScrollView(outerRectLocal, _scrollPosition, innerRect, false, false);
                    {
                        const int initialSelectedItem = -1;
                        var newSelectedItemIndex = GUI.SelectionGrid(innerRect, initialSelectedItem, listContent, 1, listStyle);
                        if (newSelectedItemIndex != initialSelectedItem)
                        {
                            onItemSelected(newSelectedItemIndex);
                            isClickedComboButton = false;
                        }
                    }
                    GUI.EndScrollView(true);
                };
            }

            if (done)
                isClickedComboButton = false;
        }

        Vector2 _scrollPosition = Vector2.zero;
        public static Action CurrentDropdownDrawer { get; set; }
    }
}
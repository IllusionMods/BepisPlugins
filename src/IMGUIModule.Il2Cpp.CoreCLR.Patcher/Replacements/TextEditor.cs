using System.Collections.Generic;
using UnityEngine;

namespace IMGUIModule.Il2Cpp.CoreCLR
{
    internal class TextEditor
    {
        public enum DblClickSnapping : byte
        {
            WORDS,
            PARAGRAPHS,
        }

        private enum CharacterType
        {
            LetterLike,
            Symbol,
            Symbol2,
            WhiteSpace,
        }

        private enum Direction
        {
            Forward,
            Backward,
        }

        private enum TextEditOp
        {
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            MoveLineStart,
            MoveLineEnd,
            MoveTextStart,
            MoveTextEnd,
            MovePageUp,
            MovePageDown,
            MoveGraphicalLineStart,
            MoveGraphicalLineEnd,
            MoveWordLeft,
            MoveWordRight,
            MoveParagraphForward,
            MoveParagraphBackward,
            MoveToStartOfNextWord,
            MoveToEndOfPreviousWord,
            SelectLeft,
            SelectRight,
            SelectUp,
            SelectDown,
            SelectTextStart,
            SelectTextEnd,
            SelectPageUp,
            SelectPageDown,
            ExpandSelectGraphicalLineStart,
            ExpandSelectGraphicalLineEnd,
            SelectGraphicalLineStart,
            SelectGraphicalLineEnd,
            SelectWordLeft,
            SelectWordRight,
            SelectToEndOfPreviousWord,
            SelectToStartOfNextWord,
            SelectParagraphBackward,
            SelectParagraphForward,
            Delete,
            Backspace,
            DeleteWordBack,
            DeleteWordForward,
            DeleteLineBack,
            Cut,
            Copy,
            Paste,
            SelectAll,
            SelectNone,
            ScrollStart,
            ScrollEnd,
            ScrollPageUp,
            ScrollPageDown,
        }

        public TouchScreenKeyboard keyboardOnScreen = null;

        public int controlID = 0;

        public GUIStyle style = GUIStyle.none;

        public bool multiline = false;

        public bool hasHorizontalCursorPos = false;

        public bool isPasswordField = false;

        internal bool m_HasFocus;

        public Vector2 scrollOffset = Vector2.zero;

        private GUIContent m_Content = new GUIContent();

        private Rect m_Position;

        private int m_CursorIndex = 0;

        private int m_SelectIndex = 0;

        private bool m_RevealCursor = false;

        public Vector2 graphicalCursorPos;

        public Vector2 graphicalSelectCursorPos;

        private bool m_MouseDragSelectsWholeWords = false;

        private int m_DblClickInitPos = 0;

        private DblClickSnapping m_DblClickSnap = DblClickSnapping.WORDS;

        private bool m_bJustSelected = false;

        private int m_iAltCursorPos = -1;

        private string oldText;

        private int oldPos;

        private int oldSelectPos;

        private static Dictionary<EventKey, TextEditOp> s_Keyactions;

        public string text
        {
            get { return m_Content.text; }
            set
            {
                m_Content.text = value ?? string.Empty;
                EnsureValidCodePointIndex(ref m_CursorIndex);
                EnsureValidCodePointIndex(ref m_SelectIndex);
            }
        }

        public Rect position
        {
            get { return m_Position; }
            set
            {
                if (!(m_Position == value))
                {
                    scrollOffset = Vector2.zero;
                    m_Position = value;
                    UpdateScrollOffset();
                }
            }
        }

        internal virtual Rect localPosition => position;

        public int cursorIndex
        {
            get { return m_CursorIndex; }
            set
            {
                int num = m_CursorIndex;
                m_CursorIndex = value;
                EnsureValidCodePointIndex(ref m_CursorIndex);
                if (m_CursorIndex != num)
                {
                    m_RevealCursor = true;
                    OnCursorIndexChange();
                }
            }
        }

        public int selectIndex
        {
            get { return m_SelectIndex; }
            set
            {
                int num = m_SelectIndex;
                m_SelectIndex = value;
                EnsureValidCodePointIndex(ref m_SelectIndex);
                if (m_SelectIndex != num)
                {
                    OnSelectIndexChange();
                }
            }
        }

        public DblClickSnapping doubleClickSnapping
        {
            get { return m_DblClickSnap; }
            set { m_DblClickSnap = value; }
        }

        public int altCursorPosition
        {
            get { return m_iAltCursorPos; }
            set { m_iAltCursorPos = value; }
        }

        public bool hasSelection => cursorIndex != selectIndex;

        public string SelectedText
        {
            get
            {
                if (cursorIndex == selectIndex)
                {
                    return "";
                }
                if (cursorIndex < selectIndex)
                {
                    return text.Substring(cursorIndex, selectIndex - cursorIndex);
                }
                return text.Substring(selectIndex, cursorIndex - selectIndex);
            }
        }

        private void ClearCursorPos()
        {
            hasHorizontalCursorPos = false;
            m_iAltCursorPos = -1;
        }

        public void OnFocus()
        {
            if (multiline)
            {
                int num = (selectIndex = 0);
                cursorIndex = num;
            }
            else
            {
                SelectAll();
            }
            m_HasFocus = true;
        }

        public void OnLostFocus()
        {
            m_HasFocus = false;
            scrollOffset = Vector2.zero;
        }

        private void GrabGraphicalCursorPos()
        {
            if (!hasHorizontalCursorPos)
            {
                graphicalCursorPos = style.GetCursorPixelPosition(
                    localPosition,
                    m_Content,
                    cursorIndex
                );
                graphicalSelectCursorPos = style.GetCursorPixelPosition(
                    localPosition,
                    m_Content,
                    selectIndex
                );
                hasHorizontalCursorPos = false;
            }
        }

        public bool HandleKeyEvent(Event e)
        {
            return HandleKeyEvent(e, textIsReadOnly: false);
        }

        internal bool HandleKeyEvent(Event e, bool textIsReadOnly)
        {
            InitKeyActions();
            EventModifiers modifiers = e.modifiers;
            e.modifiers &= ~EventModifiers.CapsLock;
            if (s_Keyactions.TryGetValue(new(e), out var operation))
            {
                PerformOperation(operation, textIsReadOnly);
                e.modifiers = modifiers;
                return true;
            }
            e.modifiers = modifiers;
            return false;
        }

        public bool DeleteLineBack()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            int num = cursorIndex;
            int num2 = num;
            while (num2-- != 0)
            {
                if (text[num2] == '\n')
                {
                    num = num2 + 1;
                    break;
                }
            }
            if (num2 == -1)
            {
                num = 0;
            }
            if (cursorIndex != num)
            {
                m_Content.text = text.Remove(num, cursorIndex - num);
                int num3 = (cursorIndex = num);
                selectIndex = num3;
                return true;
            }
            return false;
        }

        public bool DeleteWordBack()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            int num = FindEndOfPreviousWord(cursorIndex);
            if (cursorIndex != num)
            {
                m_Content.text = text.Remove(num, cursorIndex - num);
                int num2 = (cursorIndex = num);
                selectIndex = num2;
                return true;
            }
            return false;
        }

        public bool DeleteWordForward()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            int num = FindStartOfNextWord(cursorIndex);
            if (cursorIndex < text.Length)
            {
                m_Content.text = text.Remove(cursorIndex, num - cursorIndex);
                return true;
            }
            return false;
        }

        public bool Delete()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            if (cursorIndex < text.Length)
            {
                m_Content.text = text.Remove(
                    cursorIndex,
                    NextCodePointIndex(cursorIndex) - cursorIndex
                );
                return true;
            }
            return false;
        }

        public bool CanPaste()
        {
            return GUIUtility.systemCopyBuffer.Length != 0;
        }

        public bool Backspace()
        {
            if (hasSelection)
            {
                DeleteSelection();
                return true;
            }
            if (cursorIndex > 0)
            {
                int num = PreviousCodePointIndex(cursorIndex);
                m_Content.text = text.Remove(num, cursorIndex - num);
                int num2 = (cursorIndex = num);
                selectIndex = num2;
                ClearCursorPos();
                return true;
            }
            return false;
        }

        public void SelectAll()
        {
            cursorIndex = 0;
            selectIndex = text.Length;
            ClearCursorPos();
        }

        public void SelectNone()
        {
            selectIndex = cursorIndex;
            ClearCursorPos();
        }

        public bool DeleteSelection()
        {
            if (cursorIndex == selectIndex)
            {
                return false;
            }
            if (cursorIndex < selectIndex)
            {
                m_Content.text =
                    text.Substring(0, cursorIndex)
                    + text.Substring(selectIndex, text.Length - selectIndex);
                selectIndex = cursorIndex;
            }
            else
            {
                m_Content.text =
                    text.Substring(0, selectIndex)
                    + text.Substring(cursorIndex, text.Length - cursorIndex);
                cursorIndex = selectIndex;
            }
            ClearCursorPos();
            return true;
        }

        public void ReplaceSelection(string replace)
        {
            DeleteSelection();
            m_Content.text = text.Insert(cursorIndex, replace);
            selectIndex = (cursorIndex += replace.Length);
            ClearCursorPos();
        }

        public void Insert(char c)
        {
            ReplaceSelection(c.ToString());
        }

        public void MoveSelectionToAltCursor()
        {
            if (m_iAltCursorPos != -1)
            {
                int iAltCursorPos = m_iAltCursorPos;
                string selectedText = SelectedText;
                m_Content.text = text.Insert(iAltCursorPos, selectedText);
                if (iAltCursorPos < cursorIndex)
                {
                    cursorIndex += selectedText.Length;
                    selectIndex += selectedText.Length;
                }
                DeleteSelection();
                int num = (cursorIndex = iAltCursorPos);
                selectIndex = num;
                ClearCursorPos();
            }
        }

        public void MoveRight()
        {
            ClearCursorPos();
            if (selectIndex == cursorIndex)
            {
                cursorIndex = NextCodePointIndex(cursorIndex);
                DetectFocusChange();
                selectIndex = cursorIndex;
            }
            else if (selectIndex > cursorIndex)
            {
                cursorIndex = selectIndex;
            }
            else
            {
                selectIndex = cursorIndex;
            }
        }

        public void MoveLeft()
        {
            if (selectIndex == cursorIndex)
            {
                cursorIndex = PreviousCodePointIndex(cursorIndex);
                selectIndex = cursorIndex;
            }
            else if (selectIndex > cursorIndex)
            {
                selectIndex = cursorIndex;
            }
            else
            {
                cursorIndex = selectIndex;
            }
            ClearCursorPos();
        }

        public void MoveUp()
        {
            if (selectIndex < cursorIndex)
            {
                selectIndex = cursorIndex;
            }
            else
            {
                cursorIndex = selectIndex;
            }
            GrabGraphicalCursorPos();
            graphicalCursorPos.y -= 1f;
            int num = (
                selectIndex = style.GetCursorStringIndex(
                    localPosition,
                    m_Content,
                    graphicalCursorPos
                )
            );
            cursorIndex = num;
            if (cursorIndex <= 0)
            {
                ClearCursorPos();
            }
        }

        public void MoveDown()
        {
            if (selectIndex > cursorIndex)
            {
                selectIndex = cursorIndex;
            }
            else
            {
                cursorIndex = selectIndex;
            }
            GrabGraphicalCursorPos();
            graphicalCursorPos.y += style.lineHeight + 5f;
            int num = (
                selectIndex = style.GetCursorStringIndex(
                    localPosition,
                    m_Content,
                    graphicalCursorPos
                )
            );
            cursorIndex = num;
            if (cursorIndex == text.Length)
            {
                ClearCursorPos();
            }
        }

        public void MoveLineStart()
        {
            int num = ((selectIndex < cursorIndex) ? selectIndex : cursorIndex);
            int num2 = num;
            int num3;
            while (num2-- != 0)
            {
                if (text[num2] == '\n')
                {
                    num3 = (cursorIndex = num2 + 1);
                    selectIndex = num3;
                    return;
                }
            }
            num3 = (cursorIndex = 0);
            selectIndex = num3;
        }

        public void MoveLineEnd()
        {
            int num = ((selectIndex > cursorIndex) ? selectIndex : cursorIndex);
            int i = num;
            int length;
            int num2;
            for (length = text.Length; i < length; i++)
            {
                if (text[i] == '\n')
                {
                    num2 = (cursorIndex = i);
                    selectIndex = num2;
                    return;
                }
            }
            num2 = (cursorIndex = length);
            selectIndex = num2;
        }

        public void MoveGraphicalLineStart()
        {
            int num = (
                selectIndex = GetGraphicalLineStart(
                    (cursorIndex < selectIndex) ? cursorIndex : selectIndex
                )
            );
            cursorIndex = num;
        }

        public void MoveGraphicalLineEnd()
        {
            int num = (
                selectIndex = GetGraphicalLineEnd(
                    (cursorIndex > selectIndex) ? cursorIndex : selectIndex
                )
            );
            cursorIndex = num;
        }

        public void MoveTextStart()
        {
            int num = (cursorIndex = 0);
            selectIndex = num;
        }

        public void MoveTextEnd()
        {
            int num = (cursorIndex = text.Length);
            selectIndex = num;
        }

        private int IndexOfEndOfLine(int startIndex)
        {
            int num = text.IndexOf('\n', startIndex);
            return (num != -1) ? num : text.Length;
        }

        public void MoveParagraphForward()
        {
            cursorIndex = ((cursorIndex > selectIndex) ? cursorIndex : selectIndex);
            if (cursorIndex < text.Length)
            {
                int num = (cursorIndex = IndexOfEndOfLine(cursorIndex + 1));
                selectIndex = num;
            }
        }

        public void MoveParagraphBackward()
        {
            cursorIndex = ((cursorIndex < selectIndex) ? cursorIndex : selectIndex);
            if (cursorIndex > 1)
            {
                int num = (cursorIndex = text.LastIndexOf('\n', cursorIndex - 2) + 1);
                selectIndex = num;
            }
            else
            {
                int num = (cursorIndex = 0);
                selectIndex = num;
            }
        }

        public void MoveCursorToPosition(Vector2 cursorPosition)
        {
            MoveCursorToPosition_Internal(cursorPosition, Event.current.shift);
        }

        protected internal void MoveCursorToPosition_Internal(Vector2 cursorPosition, bool shift)
        {
            selectIndex = style.GetCursorStringIndex(
                localPosition,
                m_Content,
                cursorPosition + scrollOffset
            );
            if (!shift)
            {
                cursorIndex = selectIndex;
            }
            DetectFocusChange();
        }

        public void MoveAltCursorToPosition(Vector2 cursorPosition)
        {
            int cursorStringIndex = style.GetCursorStringIndex(
                localPosition,
                m_Content,
                cursorPosition + scrollOffset
            );
            m_iAltCursorPos = Mathf.Min(text.Length, cursorStringIndex);
            DetectFocusChange();
        }

        public bool IsOverSelection(Vector2 cursorPosition)
        {
            int cursorStringIndex = style.GetCursorStringIndex(
                localPosition,
                m_Content,
                cursorPosition + scrollOffset
            );
            return cursorStringIndex < Mathf.Max(cursorIndex, selectIndex)
                && cursorStringIndex > Mathf.Min(cursorIndex, selectIndex);
        }

        public void SelectToPosition(Vector2 cursorPosition)
        {
            if (!m_MouseDragSelectsWholeWords)
            {
                cursorIndex = style.GetCursorStringIndex(
                    localPosition,
                    m_Content,
                    cursorPosition + scrollOffset
                );
                return;
            }
            int index = style.GetCursorStringIndex(
                localPosition,
                m_Content,
                cursorPosition + scrollOffset
            );
            EnsureValidCodePointIndex(ref index);
            EnsureValidCodePointIndex(ref m_DblClickInitPos);
            if (m_DblClickSnap == DblClickSnapping.WORDS)
            {
                if (index < m_DblClickInitPos)
                {
                    cursorIndex = FindEndOfClassification(index, Direction.Backward);
                    selectIndex = FindEndOfClassification(m_DblClickInitPos, Direction.Forward);
                }
                else
                {
                    cursorIndex = FindEndOfClassification(index, Direction.Forward);
                    selectIndex = FindEndOfClassification(m_DblClickInitPos, Direction.Backward);
                }
            }
            else if (index < m_DblClickInitPos)
            {
                if (index > 0)
                {
                    cursorIndex = text.LastIndexOf('\n', Mathf.Max(0, index - 2)) + 1;
                }
                else
                {
                    cursorIndex = 0;
                }
                selectIndex = text.LastIndexOf('\n', Mathf.Min(text.Length - 1, m_DblClickInitPos));
            }
            else
            {
                if (index < text.Length)
                {
                    cursorIndex = IndexOfEndOfLine(index);
                }
                else
                {
                    cursorIndex = text.Length;
                }
                selectIndex = text.LastIndexOf('\n', Mathf.Max(0, m_DblClickInitPos - 2)) + 1;
            }
        }

        public void SelectLeft()
        {
            if (m_bJustSelected && cursorIndex > selectIndex)
            {
                int num = cursorIndex;
                cursorIndex = selectIndex;
                selectIndex = num;
            }
            m_bJustSelected = false;
            cursorIndex = PreviousCodePointIndex(cursorIndex);
        }

        public void SelectRight()
        {
            if (m_bJustSelected && cursorIndex < selectIndex)
            {
                int num = cursorIndex;
                cursorIndex = selectIndex;
                selectIndex = num;
            }
            m_bJustSelected = false;
            cursorIndex = NextCodePointIndex(cursorIndex);
        }

        public void SelectUp()
        {
            GrabGraphicalCursorPos();
            graphicalCursorPos.y -= 1f;
            cursorIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
        }

        public void SelectDown()
        {
            GrabGraphicalCursorPos();
            graphicalCursorPos.y += style.lineHeight + 5f;
            cursorIndex = style.GetCursorStringIndex(localPosition, m_Content, graphicalCursorPos);
        }

        public void SelectTextEnd()
        {
            cursorIndex = text.Length;
        }

        public void SelectTextStart()
        {
            cursorIndex = 0;
        }

        public void MouseDragSelectsWholeWords(bool on)
        {
            m_MouseDragSelectsWholeWords = on;
            m_DblClickInitPos = cursorIndex;
        }

        public void DblClickSnap(DblClickSnapping snapping)
        {
            m_DblClickSnap = snapping;
        }

        private int GetGraphicalLineStart(int p)
        {
            Vector2 cursorPixelPosition = style.GetCursorPixelPosition(localPosition, m_Content, p);
            cursorPixelPosition.y += 1f / GUIUtility.pixelsPerPoint;
            cursorPixelPosition.x = 0f;
            return style.GetCursorStringIndex(localPosition, m_Content, cursorPixelPosition);
        }

        private int GetGraphicalLineEnd(int p)
        {
            Vector2 cursorPixelPosition = style.GetCursorPixelPosition(localPosition, m_Content, p);
            cursorPixelPosition.y += 1f / GUIUtility.pixelsPerPoint;
            cursorPixelPosition.x += 5000f;
            return style.GetCursorStringIndex(localPosition, m_Content, cursorPixelPosition);
        }

        private int FindNextSeperator(int startPos)
        {
            int length = text.Length;
            while (startPos < length && ClassifyChar(startPos) != CharacterType.LetterLike)
            {
                startPos = NextCodePointIndex(startPos);
            }
            while (startPos < length && ClassifyChar(startPos) == CharacterType.LetterLike)
            {
                startPos = NextCodePointIndex(startPos);
            }
            return startPos;
        }

        private int FindPrevSeperator(int startPos)
        {
            startPos = PreviousCodePointIndex(startPos);
            while (startPos > 0 && ClassifyChar(startPos) != CharacterType.LetterLike)
            {
                startPos = PreviousCodePointIndex(startPos);
            }
            if (startPos == 0)
            {
                return 0;
            }
            while (startPos > 0 && ClassifyChar(startPos) == CharacterType.LetterLike)
            {
                startPos = PreviousCodePointIndex(startPos);
            }
            if (ClassifyChar(startPos) == CharacterType.LetterLike)
            {
                return startPos;
            }
            return NextCodePointIndex(startPos);
        }

        public void MoveWordRight()
        {
            cursorIndex = ((cursorIndex > selectIndex) ? cursorIndex : selectIndex);
            int num = (selectIndex = FindNextSeperator(cursorIndex));
            cursorIndex = num;
            ClearCursorPos();
        }

        public void MoveToStartOfNextWord()
        {
            ClearCursorPos();
            if (cursorIndex != selectIndex)
            {
                MoveRight();
                return;
            }
            int num = (selectIndex = FindStartOfNextWord(cursorIndex));
            cursorIndex = num;
        }

        public void MoveToEndOfPreviousWord()
        {
            ClearCursorPos();
            if (cursorIndex != selectIndex)
            {
                MoveLeft();
                return;
            }
            int num = (selectIndex = FindEndOfPreviousWord(cursorIndex));
            cursorIndex = num;
        }

        public void SelectToStartOfNextWord()
        {
            ClearCursorPos();
            cursorIndex = FindStartOfNextWord(cursorIndex);
        }

        public void SelectToEndOfPreviousWord()
        {
            ClearCursorPos();
            cursorIndex = FindEndOfPreviousWord(cursorIndex);
        }

        private CharacterType ClassifyChar(int index)
        {
            if (char.IsWhiteSpace(text, index))
            {
                return CharacterType.WhiteSpace;
            }
            if (char.IsLetterOrDigit(text, index) || text[index] == '\'')
            {
                return CharacterType.LetterLike;
            }
            return CharacterType.Symbol;
        }

        public int FindStartOfNextWord(int p)
        {
            int length = text.Length;
            if (p == length)
            {
                return p;
            }
            CharacterType characterType = ClassifyChar(p);
            if (characterType != CharacterType.WhiteSpace)
            {
                p = NextCodePointIndex(p);
                while (p < length && ClassifyChar(p) == characterType)
                {
                    p = NextCodePointIndex(p);
                }
            }
            else if (text[p] == '\t' || text[p] == '\n')
            {
                return NextCodePointIndex(p);
            }
            if (p == length)
            {
                return p;
            }
            if (text[p] == ' ')
            {
                while (p < length && ClassifyChar(p) == CharacterType.WhiteSpace)
                {
                    p = NextCodePointIndex(p);
                }
            }
            else if (text[p] == '\t' || text[p] == '\n')
            {
                return p;
            }
            return p;
        }

        private int FindEndOfPreviousWord(int p)
        {
            if (p == 0)
            {
                return p;
            }
            p = PreviousCodePointIndex(p);
            while (p > 0 && text[p] == ' ')
            {
                p = PreviousCodePointIndex(p);
            }
            CharacterType characterType = ClassifyChar(p);
            if (characterType != CharacterType.WhiteSpace)
            {
                while (p > 0 && ClassifyChar(PreviousCodePointIndex(p)) == characterType)
                {
                    p = PreviousCodePointIndex(p);
                }
            }
            return p;
        }

        public void MoveWordLeft()
        {
            cursorIndex = ((cursorIndex < selectIndex) ? cursorIndex : selectIndex);
            cursorIndex = FindPrevSeperator(cursorIndex);
            selectIndex = cursorIndex;
        }

        public void SelectWordRight()
        {
            ClearCursorPos();
            int num = selectIndex;
            if (cursorIndex < selectIndex)
            {
                selectIndex = cursorIndex;
                MoveWordRight();
                selectIndex = num;
                cursorIndex = ((cursorIndex < selectIndex) ? cursorIndex : selectIndex);
            }
            else
            {
                selectIndex = cursorIndex;
                MoveWordRight();
                selectIndex = num;
            }
        }

        public void SelectWordLeft()
        {
            ClearCursorPos();
            int num = selectIndex;
            if (cursorIndex > selectIndex)
            {
                selectIndex = cursorIndex;
                MoveWordLeft();
                selectIndex = num;
                cursorIndex = ((cursorIndex > selectIndex) ? cursorIndex : selectIndex);
            }
            else
            {
                selectIndex = cursorIndex;
                MoveWordLeft();
                selectIndex = num;
            }
        }

        public void ExpandSelectGraphicalLineStart()
        {
            ClearCursorPos();
            if (cursorIndex < selectIndex)
            {
                cursorIndex = GetGraphicalLineStart(cursorIndex);
                return;
            }
            int num = cursorIndex;
            cursorIndex = GetGraphicalLineStart(selectIndex);
            selectIndex = num;
        }

        public void ExpandSelectGraphicalLineEnd()
        {
            ClearCursorPos();
            if (cursorIndex > selectIndex)
            {
                cursorIndex = GetGraphicalLineEnd(cursorIndex);
                return;
            }
            int num = cursorIndex;
            cursorIndex = GetGraphicalLineEnd(selectIndex);
            selectIndex = num;
        }

        public void SelectGraphicalLineStart()
        {
            ClearCursorPos();
            cursorIndex = GetGraphicalLineStart(cursorIndex);
        }

        public void SelectGraphicalLineEnd()
        {
            ClearCursorPos();
            cursorIndex = GetGraphicalLineEnd(cursorIndex);
        }

        public void SelectParagraphForward()
        {
            ClearCursorPos();
            bool flag = cursorIndex < selectIndex;
            if (cursorIndex < text.Length)
            {
                cursorIndex = IndexOfEndOfLine(cursorIndex + 1);
                if (flag && cursorIndex > selectIndex)
                {
                    cursorIndex = selectIndex;
                }
            }
        }

        public void SelectParagraphBackward()
        {
            ClearCursorPos();
            bool flag = cursorIndex > selectIndex;
            if (cursorIndex > 1)
            {
                cursorIndex = text.LastIndexOf('\n', cursorIndex - 2) + 1;
                if (flag && cursorIndex < selectIndex)
                {
                    cursorIndex = selectIndex;
                }
            }
            else
            {
                int num = (cursorIndex = 0);
                selectIndex = num;
            }
        }

        public void SelectCurrentWord()
        {
            int p = cursorIndex;
            if (cursorIndex < selectIndex)
            {
                cursorIndex = FindEndOfClassification(p, Direction.Backward);
                selectIndex = FindEndOfClassification(p, Direction.Forward);
            }
            else
            {
                cursorIndex = FindEndOfClassification(p, Direction.Forward);
                selectIndex = FindEndOfClassification(p, Direction.Backward);
            }
            ClearCursorPos();
            m_bJustSelected = true;
        }

        private int FindEndOfClassification(int p, Direction dir)
        {
            if (text.Length == 0)
            {
                return 0;
            }
            if (p == text.Length)
            {
                p = PreviousCodePointIndex(p);
            }
            CharacterType characterType = ClassifyChar(p);
            do
            {
                switch (dir)
                {
                    case Direction.Backward:
                        p = PreviousCodePointIndex(p);
                        if (p == 0)
                        {
                            return (ClassifyChar(0) != characterType) ? NextCodePointIndex(0) : 0;
                        }
                        break;
                    case Direction.Forward:
                        p = NextCodePointIndex(p);
                        if (p == text.Length)
                        {
                            return text.Length;
                        }
                        break;
                }
            } while (ClassifyChar(p) == characterType);
            if (dir == Direction.Forward)
            {
                return p;
            }
            return NextCodePointIndex(p);
        }

        public void SelectCurrentParagraph()
        {
            ClearCursorPos();
            int length = text.Length;
            if (cursorIndex < length)
            {
                cursorIndex = IndexOfEndOfLine(cursorIndex) + 1;
            }
            if (selectIndex != 0)
            {
                selectIndex = text.LastIndexOf('\n', selectIndex - 1) + 1;
            }
        }

        public void UpdateScrollOffsetIfNeeded(Event evt)
        {
            if (evt.type != EventType.Repaint && evt.type != EventType.Layout)
            {
                UpdateScrollOffset();
            }
        }

        internal void UpdateScrollOffset()
        {
            int cursorStringIndex = cursorIndex;
            graphicalCursorPos = style.GetCursorPixelPosition(
                new Rect(0f, 0f, position.width, position.height),
                m_Content,
                cursorStringIndex
            );
            Rect rect = style.padding.Remove(position);
            Vector2 vector = graphicalCursorPos;
            vector.x -= style.padding.left;
            vector.y -= style.padding.top;
            Vector2 vector2 = new Vector2(
                style.CalcSize(m_Content).x,
                style.CalcHeight(m_Content, position.width)
            );
            vector2.x -= style.padding.left + style.padding.right;
            vector2.y -= style.padding.top + style.padding.bottom;
            if (vector2.x < rect.width)
            {
                scrollOffset.x = 0f;
            }
            else if (m_RevealCursor)
            {
                if (vector.x + 1f > scrollOffset.x + rect.width)
                {
                    scrollOffset.x = vector.x - rect.width + 1f;
                }
                if (vector.x < scrollOffset.x)
                {
                    scrollOffset.x = vector.x;
                }
            }
            if (vector2.y < rect.height)
            {
                scrollOffset.y = 0f;
            }
            else if (m_RevealCursor)
            {
                if (vector.y + style.lineHeight > scrollOffset.y + rect.height)
                {
                    scrollOffset.y = vector.y - rect.height + style.lineHeight;
                }
                if (vector.y < scrollOffset.y)
                {
                    scrollOffset.y = vector.y;
                }
            }
            if (scrollOffset.y > 0f && vector2.y - scrollOffset.y < rect.height)
            {
                scrollOffset.y = vector2.y - rect.height;
            }
            scrollOffset.y = ((scrollOffset.y < 0f) ? 0f : scrollOffset.y);
            m_RevealCursor = false;
        }

        public void DrawCursor(string newText)
        {
            string text = this.text;
            int num = cursorIndex;
            if (GUIUtility.compositionString.Length > 0)
            {
                m_Content.text =
                    newText.Substring(0, cursorIndex)
                    + GUIUtility.compositionString
                    + newText.Substring(selectIndex);
                num += GUIUtility.compositionString.Length;
            }
            else
            {
                m_Content.text = newText;
            }
            graphicalCursorPos = style.GetCursorPixelPosition(
                new Rect(0f, 0f, position.width, position.height),
                m_Content,
                num
            );
            Vector2 contentOffset = style.contentOffset;
            style.contentOffset -= scrollOffset;
            style.Internal_clipOffset = scrollOffset;
            GUIUtility.compositionCursorPos = GUIClip.UnclipToWindow(
                graphicalCursorPos
                    + new Vector2(position.x, position.y + style.lineHeight)
                    - scrollOffset
            );
            if (GUIUtility.compositionString.Length > 0)
            {
                style.DrawWithTextSelection(
                    position,
                    m_Content,
                    controlID,
                    cursorIndex,
                    cursorIndex + GUIUtility.compositionString.Length,
                    drawSelectionAsComposition: true
                );
            }
            else
            {
                style.DrawWithTextSelection(
                    position,
                    m_Content,
                    controlID,
                    cursorIndex,
                    selectIndex
                );
            }
            if (m_iAltCursorPos != -1)
            {
                style.DrawCursor(position, m_Content, controlID, m_iAltCursorPos);
            }
            style.contentOffset = contentOffset;
            style.Internal_clipOffset = Vector2.zero;
            m_Content.text = text;
        }

        private bool PerformOperation(TextEditOp operation, bool textIsReadOnly)
        {
            m_RevealCursor = true;
            switch (operation)
            {
                case TextEditOp.MoveLeft:
                    MoveLeft();
                    break;
                case TextEditOp.MoveRight:
                    MoveRight();
                    break;
                case TextEditOp.MoveUp:
                    MoveUp();
                    break;
                case TextEditOp.MoveDown:
                    MoveDown();
                    break;
                case TextEditOp.MoveLineStart:
                    MoveLineStart();
                    break;
                case TextEditOp.MoveLineEnd:
                    MoveLineEnd();
                    break;
                case TextEditOp.MoveWordRight:
                    MoveWordRight();
                    break;
                case TextEditOp.MoveToStartOfNextWord:
                    MoveToStartOfNextWord();
                    break;
                case TextEditOp.MoveToEndOfPreviousWord:
                    MoveToEndOfPreviousWord();
                    break;
                case TextEditOp.MoveWordLeft:
                    MoveWordLeft();
                    break;
                case TextEditOp.MoveTextStart:
                    MoveTextStart();
                    break;
                case TextEditOp.MoveTextEnd:
                    MoveTextEnd();
                    break;
                case TextEditOp.MoveParagraphForward:
                    MoveParagraphForward();
                    break;
                case TextEditOp.MoveParagraphBackward:
                    MoveParagraphBackward();
                    break;
                case TextEditOp.MoveGraphicalLineStart:
                    MoveGraphicalLineStart();
                    break;
                case TextEditOp.MoveGraphicalLineEnd:
                    MoveGraphicalLineEnd();
                    break;
                case TextEditOp.SelectLeft:
                    SelectLeft();
                    break;
                case TextEditOp.SelectRight:
                    SelectRight();
                    break;
                case TextEditOp.SelectUp:
                    SelectUp();
                    break;
                case TextEditOp.SelectDown:
                    SelectDown();
                    break;
                case TextEditOp.SelectWordRight:
                    SelectWordRight();
                    break;
                case TextEditOp.SelectWordLeft:
                    SelectWordLeft();
                    break;
                case TextEditOp.SelectToEndOfPreviousWord:
                    SelectToEndOfPreviousWord();
                    break;
                case TextEditOp.SelectToStartOfNextWord:
                    SelectToStartOfNextWord();
                    break;
                case TextEditOp.SelectTextStart:
                    SelectTextStart();
                    break;
                case TextEditOp.SelectTextEnd:
                    SelectTextEnd();
                    break;
                case TextEditOp.ExpandSelectGraphicalLineStart:
                    ExpandSelectGraphicalLineStart();
                    break;
                case TextEditOp.ExpandSelectGraphicalLineEnd:
                    ExpandSelectGraphicalLineEnd();
                    break;
                case TextEditOp.SelectParagraphForward:
                    SelectParagraphForward();
                    break;
                case TextEditOp.SelectParagraphBackward:
                    SelectParagraphBackward();
                    break;
                case TextEditOp.SelectGraphicalLineStart:
                    SelectGraphicalLineStart();
                    break;
                case TextEditOp.SelectGraphicalLineEnd:
                    SelectGraphicalLineEnd();
                    break;
                case TextEditOp.Delete:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return Delete();
                case TextEditOp.Backspace:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return Backspace();
                case TextEditOp.Cut:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return Cut();
                case TextEditOp.Copy:
                    Copy();
                    break;
                case TextEditOp.Paste:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return Paste();
                case TextEditOp.SelectAll:
                    SelectAll();
                    break;
                case TextEditOp.SelectNone:
                    SelectNone();
                    break;
                case TextEditOp.DeleteWordBack:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return DeleteWordBack();
                case TextEditOp.DeleteLineBack:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return DeleteLineBack();
                case TextEditOp.DeleteWordForward:
                    if (textIsReadOnly)
                    {
                        return false;
                    }
                    return DeleteWordForward();
                default:
                    UnityEngine.Debug.Log("Unimplemented: " + operation);
                    break;
            }
            return false;
        }

        public void SaveBackup()
        {
            oldText = text;
            oldPos = cursorIndex;
            oldSelectPos = selectIndex;
        }

        public void Undo()
        {
            m_Content.text = oldText;
            cursorIndex = oldPos;
            selectIndex = oldSelectPos;
        }

        public bool Cut()
        {
            if (isPasswordField)
            {
                return false;
            }
            Copy();
            return DeleteSelection();
        }

        public void Copy()
        {
            if (selectIndex != cursorIndex && !isPasswordField)
            {
                string systemCopyBuffer = style.Internal_GetSelectedRenderedText(
                    localPosition,
                    m_Content,
                    selectIndex,
                    cursorIndex
                );
                GUIUtility.systemCopyBuffer = systemCopyBuffer;
            }
        }

        internal Rect[] GetHyperlinksRect()
        {
            return style.Internal_GetHyperlinksRect(localPosition, m_Content);
        }

        private static string ReplaceNewlinesWithSpaces(string value)
        {
            value = value.Replace("\r\n", " ");
            value = value.Replace('\n', ' ');
            value = value.Replace('\r', ' ');
            return value;
        }

        public bool Paste()
        {
            string text = GUIUtility.systemCopyBuffer;
            if (text != "")
            {
                if (!multiline)
                {
                    text = ReplaceNewlinesWithSpaces(text);
                }
                ReplaceSelection(text);
                return true;
            }
            return false;
        }

        private static void MapKey(string key, TextEditOp action)
        {
            s_Keyactions[new(Event.KeyboardEvent(key))] = action;
        }

        private void InitKeyActions()
        {
            if (s_Keyactions == null)
            {
                s_Keyactions = new Dictionary<EventKey, TextEditOp>();
                MapKey("left", TextEditOp.MoveLeft);
                MapKey("right", TextEditOp.MoveRight);
                MapKey("up", TextEditOp.MoveUp);
                MapKey("down", TextEditOp.MoveDown);
                MapKey("#left", TextEditOp.SelectLeft);
                MapKey("#right", TextEditOp.SelectRight);
                MapKey("#up", TextEditOp.SelectUp);
                MapKey("#down", TextEditOp.SelectDown);
                MapKey("delete", TextEditOp.Delete);
                MapKey("backspace", TextEditOp.Backspace);
                MapKey("#backspace", TextEditOp.Backspace);
                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                {
                    MapKey("^left", TextEditOp.MoveGraphicalLineStart);
                    MapKey("^right", TextEditOp.MoveGraphicalLineEnd);
                    MapKey("&left", TextEditOp.MoveWordLeft);
                    MapKey("&right", TextEditOp.MoveWordRight);
                    MapKey("&up", TextEditOp.MoveParagraphBackward);
                    MapKey("&down", TextEditOp.MoveParagraphForward);
                    MapKey("%left", TextEditOp.MoveGraphicalLineStart);
                    MapKey("%right", TextEditOp.MoveGraphicalLineEnd);
                    MapKey("%up", TextEditOp.MoveTextStart);
                    MapKey("%down", TextEditOp.MoveTextEnd);
                    MapKey("#home", TextEditOp.SelectTextStart);
                    MapKey("#end", TextEditOp.SelectTextEnd);
                    MapKey("#^left", TextEditOp.ExpandSelectGraphicalLineStart);
                    MapKey("#^right", TextEditOp.ExpandSelectGraphicalLineEnd);
                    MapKey("#^up", TextEditOp.SelectParagraphBackward);
                    MapKey("#^down", TextEditOp.SelectParagraphForward);
                    MapKey("#&left", TextEditOp.SelectWordLeft);
                    MapKey("#&right", TextEditOp.SelectWordRight);
                    MapKey("#&up", TextEditOp.SelectParagraphBackward);
                    MapKey("#&down", TextEditOp.SelectParagraphForward);
                    MapKey("#%left", TextEditOp.ExpandSelectGraphicalLineStart);
                    MapKey("#%right", TextEditOp.ExpandSelectGraphicalLineEnd);
                    MapKey("#%up", TextEditOp.SelectTextStart);
                    MapKey("#%down", TextEditOp.SelectTextEnd);
                    MapKey("%a", TextEditOp.SelectAll);
                    MapKey("%x", TextEditOp.Cut);
                    MapKey("%c", TextEditOp.Copy);
                    MapKey("%v", TextEditOp.Paste);
                    MapKey("^d", TextEditOp.Delete);
                    MapKey("^h", TextEditOp.Backspace);
                    MapKey("^b", TextEditOp.MoveLeft);
                    MapKey("^f", TextEditOp.MoveRight);
                    MapKey("^a", TextEditOp.MoveLineStart);
                    MapKey("^e", TextEditOp.MoveLineEnd);
                    MapKey("&delete", TextEditOp.DeleteWordForward);
                    MapKey("&backspace", TextEditOp.DeleteWordBack);
                    MapKey("%backspace", TextEditOp.DeleteLineBack);
                }
                else
                {
                    MapKey("home", TextEditOp.MoveGraphicalLineStart);
                    MapKey("end", TextEditOp.MoveGraphicalLineEnd);
                    MapKey("%left", TextEditOp.MoveWordLeft);
                    MapKey("%right", TextEditOp.MoveWordRight);
                    MapKey("%up", TextEditOp.MoveParagraphBackward);
                    MapKey("%down", TextEditOp.MoveParagraphForward);
                    MapKey("^left", TextEditOp.MoveToEndOfPreviousWord);
                    MapKey("^right", TextEditOp.MoveToStartOfNextWord);
                    MapKey("^up", TextEditOp.MoveParagraphBackward);
                    MapKey("^down", TextEditOp.MoveParagraphForward);
                    MapKey("#^left", TextEditOp.SelectToEndOfPreviousWord);
                    MapKey("#^right", TextEditOp.SelectToStartOfNextWord);
                    MapKey("#^up", TextEditOp.SelectParagraphBackward);
                    MapKey("#^down", TextEditOp.SelectParagraphForward);
                    MapKey("#home", TextEditOp.SelectGraphicalLineStart);
                    MapKey("#end", TextEditOp.SelectGraphicalLineEnd);
                    MapKey("^delete", TextEditOp.DeleteWordForward);
                    MapKey("^backspace", TextEditOp.DeleteWordBack);
                    MapKey("%backspace", TextEditOp.DeleteLineBack);
                    MapKey("^a", TextEditOp.SelectAll);
                    MapKey("^x", TextEditOp.Cut);
                    MapKey("^c", TextEditOp.Copy);
                    MapKey("^v", TextEditOp.Paste);
                    MapKey("#delete", TextEditOp.Cut);
                    MapKey("^insert", TextEditOp.Copy);
                    MapKey("#insert", TextEditOp.Paste);
                }
            }
        }

        public void DetectFocusChange()
        {
            OnDetectFocusChange();
        }

        internal virtual void OnDetectFocusChange()
        {
            if (m_HasFocus && controlID != GUIUtility.keyboardControl)
            {
                OnLostFocus();
            }
            if (!m_HasFocus && controlID == GUIUtility.keyboardControl)
            {
                OnFocus();
            }
        }

        internal virtual void OnCursorIndexChange() { }

        internal virtual void OnSelectIndexChange() { }

        private void ClampTextIndex(ref int index)
        {
            index = Mathf.Clamp(index, 0, text.Length);
        }

        private void EnsureValidCodePointIndex(ref int index)
        {
            ClampTextIndex(ref index);
            if (!IsValidCodePointIndex(index))
            {
                index = NextCodePointIndex(index);
            }
        }

        private bool IsValidCodePointIndex(int index)
        {
            if (index < 0 || index > text.Length)
            {
                return false;
            }
            if (index == 0 || index == text.Length)
            {
                return true;
            }
            return !char.IsLowSurrogate(text[index]);
        }

        private int PreviousCodePointIndex(int index)
        {
            if (index > 0)
            {
                index--;
            }
            while (index > 0 && char.IsLowSurrogate(text[index]))
            {
                index--;
            }
            return index;
        }

        private int NextCodePointIndex(int index)
        {
            if (index < text.Length)
            {
                index++;
            }
            while (index < text.Length && char.IsLowSurrogate(text[index]))
            {
                index++;
            }
            return index;
        }

        private class EventKey
        {
            private readonly Event ev;

            public EventKey(Event ev)
            {
                this.ev = ev;
            }

            public override int GetHashCode()
            {
                return ev.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != GetType())
                    return false;

                return ev.Equals(((EventKey)obj).ev);
            }
        }
    }
}

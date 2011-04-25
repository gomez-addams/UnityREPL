//-----------------------------------------------------------------
//  GUIHelper v0.12
//  Copyright 2009-2011 MrJoy, Inc.
//  All rights reserved
//
//-----------------------------------------------------------------
// Helper for UnityGUI tasks, particularly editor-related.
//-----------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections;

public class UnityREPLHelper {
  private static Hashtable styleCache = new Hashtable();
  public static GUIStyle CachedStyle(string name) {
    if(!styleCache.ContainsKey(name))
      styleCache[name] = GUI.skin.GetStyle(name);
    return (GUIStyle)styleCache[name];
  }

  public static NumberedEditorState NumberedTextArea(string controlName, NumberedEditorState editorState) {
    // This is a WAG about Unity's box model.  Seems to work though, so... yeah.
    float effectiveWidgetHeight = 7 * GUI.skin.label.lineHeight
      + GUI.skin.label.padding.top + GUI.skin.label.padding.bottom
    ;
    Rect r = EditorGUILayout.BeginVertical();
    if(r.width > 0) {
      editorState.scrollViewWidth = r.width;
      editorState.scrollViewHeight = r.height;
    }

    editorState.scrollPos = GUILayout.BeginScrollView(editorState.scrollPos, false, false, CachedStyle("HorizontalScrollbar"), CachedStyle("VerticalScrollbar"), CachedStyle("TextField"), GUILayout.Height(effectiveWidgetHeight));
      GUILayout.BeginHorizontal();
        GUILayout.Label(editorState.lineNumberingContent, NumberedEditorStyles.LineNumbering);
        GUIContent txt = new GUIContent(editorState.text);
        GUIContent dTxt = new GUIContent(editorState.dummyText);
        float minW, maxW;
        NumberedEditorStyles.NumberedEditor.CalcMinMaxWidth(dTxt, out minW, out maxW);
        GUI.SetNextControlName(controlName);
        Rect editorRect = GUILayoutUtility.GetRect(txt, NumberedEditorStyles.NumberedEditor, GUILayout.Width(maxW));
        editorRect.width = maxW;
        bool wasMouseDrag = Event.current.type == EventType.MouseDrag;
        bool wasRelevantEvent = wasMouseDrag || Event.current.type == EventType.KeyDown;
        editorState.text = GUI.TextField(editorRect, editorState.text, NumberedEditorStyles.NumberedEditor);

        if ((GUI.GetNameOfFocusedControl() == controlName) &&
            wasRelevantEvent) {
          int editorId = GUIUtility.keyboardControl;
          TextEditor te = GUIUtility.QueryStateObject(typeof(System.Object), editorId) as TextEditor;
          int pos = te.pos; // TODO: How does this play with keyboard selection?  We want the actual cursor pos, not necessarily the right-end.
          if(pos != editorState.lastPos) {
            Vector2 cursorPixelPos = NumberedEditorStyles.NumberedEditor.GetCursorPixelPosition(editorRect, txt, pos);
            cursorPixelPos.y -= 1; // 0-align...
            float yBuffer = NumberedEditorStyles.NumberedEditor.lineHeight * 2;
            float xBuffer = 40f; // TODO: Make this a little less arbitrary?
            if(wasMouseDrag) {
              yBuffer = 0;
              xBuffer = 0;
            }

            if(editorState.scrollViewWidth > 0) {
              if(cursorPixelPos.y + yBuffer > editorState.scrollPos.y + editorState.scrollViewHeight - NumberedEditorStyles.NumberedEditor.lineHeight)
                editorState.scrollPos.y = cursorPixelPos.y + yBuffer + NumberedEditorStyles.NumberedEditor.lineHeight - editorState.scrollViewHeight;
              if(cursorPixelPos.y - yBuffer < editorState.scrollPos.y)
                editorState.scrollPos.y = cursorPixelPos.y - yBuffer;

              if(cursorPixelPos.x + xBuffer > editorState.scrollPos.x + editorState.scrollViewWidth)
                editorState.scrollPos.x = cursorPixelPos.x + xBuffer - editorState.scrollViewWidth;
              if(cursorPixelPos.x - xBuffer < editorState.scrollPos.x)
                editorState.scrollPos.x = cursorPixelPos.x - xBuffer;
            }
          }
          editorState.lastPos = pos;
        }
      GUILayout.EndHorizontal();
    GUILayout.EndScrollView();
    EditorGUILayout.EndVertical();

    return editorState;
  }
}

public static class GUIStyleExtensions {
  public static GUIStyle NoBackgroundImages(this GUIStyle style) {
    style.normal.background = null;
    style.active.background = null;
    style.hover.background = null;
    style.focused.background = null;
    style.onNormal.background = null;
    style.onActive.background = null;
    style.onHover.background = null;
    style.onFocused.background = null;
    return style;
  }

  public static GUIStyle BaseTextColor(this GUIStyle style, Color c) {
    style.normal.textColor =
      style.active.textColor =
      style.hover.textColor =
      style.focused.textColor =
      style.onNormal.textColor =
      style.onActive.textColor =
      style.onHover.textColor =
      style.onFocused.textColor =
      c;
    return style;
  }

  public static GUIStyle ResetBoxModel(this GUIStyle style) {
    style.border = new RectOffset();
    style.margin = new RectOffset();
    style.padding = new RectOffset();
    style.overflow = new RectOffset();

    return style;
  }

  public static GUIStyle Padding(this GUIStyle style, int left, int right, int top, int bottom) {
    style.padding = new RectOffset(left, right, top, bottom);
    return style;
  }

  public static GUIStyle Margin(this GUIStyle style, int left, int right, int top, int bottom) {
    style.margin = new RectOffset(left, right, top, bottom);
    return style;
  }

  public static GUIStyle Named(this GUIStyle style, string name) {
    style.name = name;
    return style;
  }

  public static GUIStyle ClipText(this GUIStyle style) {
    style.clipping = TextClipping.Clip;
    return style;
  }
}

public class NumberedEditorStyles {
  private static GUIStyle _LineNumbering = null;
  public static GUIStyle LineNumbering {
    get {
      if(_LineNumbering == null) {
        _LineNumbering = new GUIStyle(EditorStyles.textField) {
          alignment = TextAnchor.UpperRight,
          fixedWidth = 0,
          fixedHeight = 0,
          wordWrap = false,
          stretchWidth = false,
          stretchHeight = true,
          imagePosition = ImagePosition.TextOnly
        }
          .Named("LineNumbering")
          .NoBackgroundImages()
          .ResetBoxModel()
          .Padding(5, 0, 0, 0)
          .BaseTextColor(new Color(0.5f, 0.5f, 0.5f, 1f))
          .ClipText();
      }
      return _LineNumbering;
    }
  }

  private static GUIStyle _NumberedEditor = null;
  public static GUIStyle NumberedEditor {
    get {
      if(_NumberedEditor == null) {
        _NumberedEditor = new GUIStyle(LineNumbering) {
          alignment = TextAnchor.UpperLeft,
          stretchWidth = true,
          stretchHeight = true
        }
          .Named("NumberedEditor")
          .ResetBoxModel()
          .Padding(0, 4, 0, 0)
          .Margin(5, 0, 0, 0)
          .BaseTextColor(new Color(0f, 0f, 0f, 1f))
          .ClipText();
      }
      return _NumberedEditor;
    }
  }

  private static GUIStyle _DummyStyle = null;
  public static GUIStyle DummyStyle {
    get {
      if(_DummyStyle == null) {
        _DummyStyle = new GUIStyle(NumberedEditor)
          .Named("DummyStyle")
          .BaseTextColor(new Color(0f, 0f, 0f, 0f));
      }
      return _DummyStyle;
    }
  }
}

public class LogEntryStyles {
  private static GUIStyle _Default = null;
  public static GUIStyle Default {
    get {
      if(_Default == null) {
        _Default = new GUIStyle("Label")
          .Named("DummyStyle")
          .BaseTextColor(new Color(0f, 0f, 0f, 1f));
      }
      return _Default;
    }
  }

  private static GUIStyle _DefaultCommandStyle = null;
  public static GUIStyle DefaultCommandStyle {
    get {
      if(_DefaultCommandStyle == null) {
        _DefaultCommandStyle = new GUIStyle(Default)
          .Named("DefaultCommandStyle")
          .BaseTextColor(new Color(0f, 0f, 0f, 1f));
      }
      return _DefaultCommandStyle;
    }
  }

  private static GUIStyle _FoldoutCommandStyle = null;
  public static GUIStyle FoldoutCommandStyle {
    get {
      if(_FoldoutCommandStyle == null) {
        _FoldoutCommandStyle = new GUIStyle(EditorStyles.foldout)
          .Named("FoldoutCommandStyle")
          .BaseTextColor(DefaultCommandStyle.active.textColor);
      }
      return _FoldoutCommandStyle;
    }
  }

  private static GUIStyle _OutputStyle = null;
  public static GUIStyle OutputStyle {
    get {
      if(_OutputStyle == null) {
        _OutputStyle = new GUIStyle(Default)
          .Named("OutputStyle")
          .BaseTextColor(new Color(0f, 0.5f, 0f, 1f));
      }
      return _OutputStyle;
    }
  }

  private static GUIStyle _EvaluationErrorStyle = null;
  public static GUIStyle EvaluationErrorStyle {
    get {
      if(_EvaluationErrorStyle == null) {
        _EvaluationErrorStyle = new GUIStyle(Default)
          .Named("EvaluationErrorStyle")
          .BaseTextColor(new Color(0.5f, 0f, 0f, 1f));
      }
      return _EvaluationErrorStyle;
    }
  }

  private static GUIStyle _SystemConsoleStyle = null;
  public static GUIStyle SystemConsoleStyle {
    get {
      if(_SystemConsoleStyle == null) {
        _SystemConsoleStyle = new GUIStyle(Default)
          .Named("SystemConsoleStyle")
          .BaseTextColor(new Color(0.5f, 0.5f, 0f, 1f));
      }
      return _SystemConsoleStyle;
    }
  }

  private static GUIStyle _ConsoleLogStyle = null;
  public static GUIStyle ConsoleLogStyle {
    get {
      if(_ConsoleLogStyle == null) {
        _ConsoleLogStyle = new GUIStyle(Default)
          .Named("ConsoleLogStyle")
          .BaseTextColor(new Color(0.25f, 0.25f, 0.25f, 1f));
      }
      return _ConsoleLogStyle;
    }
  }

  private static GUIStyle _ConsoleLogConditionStyle = null;
  public static GUIStyle ConsoleLogConditionStyle {
    get {
      if(_ConsoleLogConditionStyle == null) {
        _ConsoleLogConditionStyle = new GUIStyle(ConsoleLogStyle)
          .Named("ConsoleLogConditionStyle");
      }
      return _ConsoleLogConditionStyle;
    }
  }

  private static GUIStyle _ConsoleLogStackTraceStyle = null;
  public static GUIStyle ConsoleLogStackTraceStyle {
    get {
      if(_ConsoleLogStackTraceStyle == null) {
        _ConsoleLogStackTraceStyle = new GUIStyle(ConsoleLogStyle)
          .Named("ConsoleLogStackTraceStyle")
          .BaseTextColor(new Color(0.5f, 0f, 0f, 1f));
      }
      return _ConsoleLogStackTraceStyle;
    }
  }

}

[System.Serializable]
public class NumberedEditorState {
  public Vector2 scrollPos;
  public float scrollViewWidth, scrollViewHeight;
  public int lastPos;
  public bool textChanged = false;
  private string _text = "";
  public string text {
    get { return _text; }
    set {
      if(_text != value) {
        _text = value;
        _lineNumberingContent = null;
        _textContent = null;
        _dummyText = null;
        textChanged = true;
      }
    }
  }

  private GUIContent _textContent = null;
  public GUIContent textContent {
    get {
      if(_textContent == null)
        _textContent = new GUIContent(text);
      return _textContent;
    }
  }

  private string _dummyText = null;
  public string dummyText {
    get {
      return _dummyText;
    }
  }

  private GUIContent _lineNumberingContent = null;
  public GUIContent lineNumberingContent {
    get {
      // Unity likes to ignore trailing space when sizing content, which is a
      // problem for us, so we construct a version of our content that has a .
      // at the end of each line -- small enough to not consume too much extra
      // width, but not a space, so we can use that for sizing later on.
      if(_lineNumberingContent == null) {
        string[] linesRaw = text.Split('\n');
        int lines = linesRaw.Length;
        if(lines == 0) lines = 1;

        StringBuilder sb = new StringBuilder();
        for(int j = 0; j < lines; j++)
          sb.Append(linesRaw[j]).Append(".").Append("\n");
        _dummyText = sb.ToString();

        // While we're at it, build a single string with all our line numbers.
        sb.Length = 0;
        for(int j = 0; j < lines; j++)
          sb.Append(j+1).Append('\n');

        _lineNumberingContent = new GUIContent(sb.ToString());
      }
      return _lineNumberingContent;
    }
  }
}

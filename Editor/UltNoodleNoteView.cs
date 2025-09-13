using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using NoodledEvents;

public class UltNoodleNoteView : GraphElement
{
    private readonly Label _displayLabel;
    private readonly TextField _editField;

    private bool _isEditing;

    private UltNoodleNoteData _data;

    public UltNoodleNoteView(UltNoodleNoteData data, SerializedBowl bowl)
    {
        _data = data;

        capabilities = Capabilities.Selectable | Capabilities.Movable | Capabilities.Resizable;
        pickingMode = PickingMode.Position;
        focusable = true;

        style.position = Position.Absolute;
        style.left = _data.Position.x;
        style.top = _data.Position.y;
        style.width = (_data.Size == default) ? 200f : _data.Size.x;
        style.height = (_data.Size == default) ? 140f : _data.Size.y;

        style.backgroundColor = new Color(1f, 0.96f, 0.66f);
        style.borderTopWidth = style.borderLeftWidth = style.borderRightWidth = style.borderBottomWidth = 1;
        style.borderTopColor = style.borderLeftColor = style.borderRightColor = style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);
        style.paddingLeft = style.paddingTop = style.paddingRight = style.paddingBottom = 6;

        // display label (non-edit mode)
        _displayLabel = new Label(_data.Text)
        {
            name = "displayLabel",
            tooltip = "Double-click to edit"
        };
        _displayLabel.style.whiteSpace = WhiteSpace.Normal;
        _displayLabel.style.color = Color.black;
        _displayLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        _displayLabel.style.flexGrow = 1;
        _displayLabel.style.overflow = Overflow.Hidden;
        Add(_displayLabel);

        // edit field (hidden until editing)
        _editField = new TextField { multiline = true, name = "editField", value = _data.Text };
        _editField.style.color = Color.black;
        _editField.style.display = DisplayStyle.None;
        _editField.style.flexGrow = 1;
        _editField.style.whiteSpace = WhiteSpace.Normal;

        Add(_editField);

        _displayLabel.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
            {
                BeginEdit();
                evt.StopPropagation();
            }

            if (evt.button == 1 && evt.clickCount == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete Note"), false, () =>
                {
                    Undo.RecordObject(bowl, "Delete Note");
                    bowl.NoteDatas.Remove(_data);
                    RemoveFromHierarchy();
                });
                menu.ShowAsContext();
                evt.StopPropagation();
            }
        });

        _editField.RegisterCallback<BlurEvent>(_ => EndEdit(save: true));
        _editField.RegisterCallback<KeyDownEvent>(evt =>
        {
            // commit on Ctrl/Cmd + Enter, escape to cancel
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.Return)
            {
                EndEdit(save: true);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                EndEdit(save: false);
                evt.StopPropagation();
            }
        });

        RegisterCallback<GeometryChangedEvent>(e =>
        {
            _data.Position = new Vector2(style.left.value.value, style.top.value.value);
            _data.Size = new Vector2(style.width.value.value, style.height.value.value);
        });
    }

    private void BeginEdit()
    {
        if (_isEditing) return;
        _isEditing = true;

        _editField.value = _displayLabel.text;
        _displayLabel.style.display = DisplayStyle.None;
        _editField.style.display = DisplayStyle.Flex;

        _editField.Focus();
        _editField.SelectAll();
    }

    private void EndEdit(bool save)
    {
        if (!_isEditing) return;
        if (save)
        {
            _displayLabel.text = _editField.value;
            _data.Text = _editField.value;
        }
        _editField.style.display = DisplayStyle.None;
        _displayLabel.style.display = DisplayStyle.Flex;
        _isEditing = false;
    }
}

[Serializable]
public class UltNoodleNoteData
{
    public string Text;
    public Vector2 Position;
    public Vector2 Size;

    public UltNoodleNoteData(string text, Vector2 position, Vector2 size = default)
    {
        Text = text;
        Position = position;
        Size = size;
    }
}

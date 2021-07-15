using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ToggleButtonGroup : MonoBehaviour
{
    public List<Button> _buttons = new List<Button>();

    private void Start()
    {
        _buttons.ForEach(b => b.onClick.AddListener(() => OnButtonClick(b)));
    }

    private void OnButtonClick(Button button)
    {
        SetAllButtonInteractable();

        button.interactable = false;
    }

    private void SetAllButtonInteractable()
    {
        _buttons.ForEach(b => b.interactable = true);
    }
}

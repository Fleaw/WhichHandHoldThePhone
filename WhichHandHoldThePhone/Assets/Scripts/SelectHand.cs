using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectHand : MonoBehaviour
{
    public enum Hand
    {
        Left,
        Right,
        Both,
        None
    }

    private Hand _selectedHand = Hand.None;

    public List<Button> _buttons = new List<Button>();

    public Hand SelectedHand { get => _selectedHand; }

    public void Select(int hand)
    {
        if (EventSystem.current.currentSelectedGameObject == null) return;

        ResetAllButtonsToWhite();

        if(hand == (int)_selectedHand)
        {
            _selectedHand = Hand.None;
            return;
        }

        _selectedHand = (Hand)hand;

        EventSystem.current.currentSelectedGameObject.GetComponent<Image>().color = Color.green;
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        _buttons.ForEach(b => b.interactable = interactable);
    }

    private void ResetAllButtonsToWhite()
    {
        _buttons.ForEach(b => b.GetComponent<Image>().color = Color.white );
    }
}

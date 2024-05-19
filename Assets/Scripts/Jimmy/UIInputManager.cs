using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInputManager : MonoBehaviour
{
    private UIManager manager;

    [Header("Component References")]
    [SerializeField] private GridLayoutGroup grid;

    [Header("Pool References")]
    [SerializeField] private int poolSize;
    [SerializeField] private GameObject UIInputPrefab;
    private List<UIInput> pool = new List<UIInput>();
    private List<UIInput> objectsInUse = new List<UIInput>();

    [Header("Sprites References")]
    public InputSprites sprites;

    public void Initialize(UIManager manager)
    {
        this.manager = manager;

        GameObject temp;

        for (int i = 0; i < poolSize; i++)
        {
            temp = Instantiate(UIInputPrefab, transform);
            temp.SetActive(false);
            pool.Add(temp.GetComponent<UIInput>());
        }
    }

    public void SetUIInput(PlayerManager player)
    {
        for (int i = objectsInUse.Count - 1; i > -1; --i)
        {
            ReturnObjectToPool(objectsInUse[i]);
        }

        if (player != null)
        {
            if (!player.isLadder && !player.isLadderTrigger)
            {
                if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                {
                    GetObjectFromPool(sprites.southGamepad, "Sauter");
                }
                else
                {
                    GetObjectFromPool(sprites.southKeyboard, "Sauter");
                }
            }
            else
            {
                if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                {
                    GetObjectFromPool(sprites.southGamepad, "Interagir");
                }
                else
                {
                    GetObjectFromPool(sprites.southKeyboard, "Interagir");
                }
            }

            if (!player.isAiming)
            {
                if (player.heldObject != null)
                {
                    if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                    {
                        GetObjectFromPool(sprites.westGamepad, "Lâcher");
                    }
                    else
                    {
                        GetObjectFromPool(sprites.westKeyboard, "Lâcher");
                    }

                    if (player.equippedObject == null)
                    {
                        if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                        {
                            GetObjectFromPool(sprites.northGamepad, "Équiper");
                        }
                        else
                        {
                            GetObjectFromPool(sprites.northKeyboard, "Équiper");
                        }
                    }
                    else
                    {
                        if (player.equippedObject.type == StateManager.Type.Mushroom)
                        {
                            MushroomManager mushroom = (MushroomManager)player.equippedObject;
                            if (mushroom.stateToApply == StateManager.State.Sticky)
                            {
                                if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                                {
                                    GetObjectFromPool(sprites.rightShoulderGamepad, "Sticky");
                                }
                                else
                                {
                                    GetObjectFromPool(sprites.rightShoulderKeyboard, "Sticky");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (player.trigger.current != null && player.trigger.triggeredObjectsList.Count > 0)
                    {
                        if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                        {
                            GetObjectFromPool(sprites.westGamepad, "Attraper");
                        }
                        else
                        {
                            GetObjectFromPool(sprites.westKeyboard, "Attraper");
                        }
                    }

                    if (player.equippedObject != null)
                    {
                        if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                        {
                            GetObjectFromPool(sprites.northGamepad, "Déséquiper");
                        }
                        else
                        {
                            GetObjectFromPool(sprites.northKeyboard, "Déséquiper");
                        }
                    }
                }

                if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                {
                    GetObjectFromPool(sprites.leftShoulderGamepad, "Viser");
                }
                else
                {
                    GetObjectFromPool(sprites.leftShoulderKeyboard, "Viser");
                }
            }
            else
            {
                if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                {
                    GetObjectFromPool(sprites.eastGamepad, "Transfert");
                }
                else
                {
                    GetObjectFromPool(sprites.eastKeyboard, "Transfert");
                }

                if (player.heldObject != null)
                {
                    if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                    {
                        GetObjectFromPool(sprites.westGamepad, "Lancer");
                    }
                    else
                    {
                        GetObjectFromPool(sprites.westKeyboard, "Lancer");
                    }
                }

                if (player.equippedObject != null)
                {
                    if (player.equippedObject.type == StateManager.Type.Mushroom)
                    {
                        MushroomManager mushroom = (MushroomManager)player.equippedObject;
                        if (mushroom.stateToApply == StateManager.State.Sticky)
                        {
                            if (manager.gameManager.previousControlScheme == manager.gameManager.gamepad)
                            {
                                GetObjectFromPool(sprites.rightShoulderGamepad, "Sticky");
                            }
                            else
                            {
                                GetObjectFromPool(sprites.rightShoulderKeyboard, "Sticky");
                            }
                        }
                    }
                }
            }
        }
    }

    public UIInput GetObjectFromPool(Sprite sprite, string text)
    {
        if (pool.Count > 0)
        {
            UIInput pulledObject = pool[0];
            pool.RemoveAt(0);
            objectsInUse.Add(pulledObject);
            pulledObject.sprite.sprite = sprite;
            pulledObject.sprite.fillMethod = Image.FillMethod.Radial360;
            pulledObject.sprite.fillAmount = 1f;
            pulledObject.sprite.preserveAspect = true;
            pulledObject.text.text = text;
            pulledObject.transform.parent = grid.transform;
            pulledObject.gameObject.SetActive(true);

            return pulledObject;
        }

        return null;
    }

    public void ReturnObjectToPool(UIInput objectToReturn)
    {
        objectToReturn.transform.position = transform.position;
        objectToReturn.transform.rotation = transform.rotation;
        objectToReturn.transform.parent = transform;

        objectToReturn.gameObject.SetActive(false);
        objectsInUse.Remove(objectToReturn);
        pool.Add(objectToReturn);
    }

    [Serializable]
    public class InputSprites
    {
        public Sprite southGamepad;
        public Sprite southKeyboard;
        public Sprite eastGamepad;
        public Sprite eastKeyboard;
        public Sprite northGamepad;
        public Sprite northKeyboard;
        public Sprite westGamepad;
        public Sprite westKeyboard;
        public Sprite rightShoulderGamepad;
        public Sprite rightShoulderKeyboard;
        public Sprite leftShoulderGamepad;
        public Sprite leftShoulderKeyboard;
        public Sprite rightTriggerGamepad;
        public Sprite rightTriggerKeyboard;
        public Sprite leftTriggerGamepad;
        public Sprite leftTriggerKeyboard;
    }
}

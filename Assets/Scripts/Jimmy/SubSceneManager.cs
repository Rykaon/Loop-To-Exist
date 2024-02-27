using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SubSceneManager : MonoBehaviour
{
    public static SubSceneManager instance;
    [Header("Entities References")]
    [SerializeField] public List<StateManager> entities;

    public List<PlayerManager> playerList { get; private set; }
    public List<ItemManager> itemList { get; private set; }
    public List<ObjectManager> objectList { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
        
        playerList = entities.OfType<PlayerManager>().ToList();
        itemList = entities.OfType<ItemManager>().ToList();
        objectList = entities.OfType<ObjectManager>().ToList();

        GameManager.instance.Init();
    }
}

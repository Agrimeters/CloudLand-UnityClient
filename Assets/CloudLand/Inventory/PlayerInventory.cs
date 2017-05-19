﻿using UnityEngine;
using Org.Dragonet.Cloudland.Net.Protocol;

public class PlayerInventory : Inventory {

    public Transform hand;

    public SerializedItem[] craftingInput = new SerializedItem[4];
    public SerializedItem[] craftingOutput = new SerializedItem[1];

    public GUISkin skin;

    private const int barWidth = 364;
    private const int barWidthHalf = barWidth / 2;

    public int currentSelection = 0;

    // Use this for initialization
    void Start() {
        items = new SerializedItem[9]; // 9 for initial bar
    }

    void Update()
    {
        float change = Input.GetAxis("Mouse ScrollWheel");

        int prev = currentSelection;
        if (change != 0)
            currentSelection = (currentSelection - (change > 0f ? 1 : -1)) % 9;

        if (currentSelection < 0) currentSelection += 9;

        if(prev != currentSelection)
        {
            ClientHotbarSelectionMessage msg = new ClientHotbarSelectionMessage();
            msg.Index = currentSelection;
            ClientComponent.INSTANCE.GetClient().sendMessage(msg);
        }

        if (items[currentSelection] == null || items[currentSelection].Id == 0)
        {
            SetArmShown(true);
            SetBlockShown(false, 0);
        } else
        {
            if (items[currentSelection].Id < Block.prototypes.Length)
            {
                SetBlockShown(true, items[currentSelection].Id);
                SetArmShown(false);
            } else
            {
                SetArmShown(true);
            }
        }
    }

    void LateUpdate()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            WindowManager.INSTANCE.openInventoryWindow();
        }
    }

    void OnGUI()
    {
        GUI.skin = skin;
        GUI.Window(0, new Rect(Screen.width / 2f - barWidthHalf, Screen.height - 44, barWidth, 44), inventoryFunc, "");
    }

    const int padding = 6;

    void inventoryFunc(int id)
    {
        int slotWidth = 32 + 8;
        for (int slot = 0; slot < 9; slot++)
        {
            if (items[slot] != null && items[slot].Id != 0)
            {
                GUI.DrawTexture(new Rect(padding + slotWidth * slot, 6, 32, 32), Inventory.getItemTexture(items[slot].Id));
                GUI.Label(new Rect(padding + slotWidth * slot + (slotWidth / 2), slotWidth / 2, 32, 32), items[slot].Count.ToString());
            }
            if (slot == currentSelection)
            {
                GUI.Box(new Rect(slotWidth * slot - 2, 0, 48, 48), "");
            }
        }
    }

    public SerializedItem GetSelectedItem()
    {
        return items[currentSelection];
    }

    private int blockShownInfo = 0;
    private bool modelCreated = false;

    public void SetBlockShown(bool show, int id)
    {
        GameObject block = hand.FindChild("Block").gameObject;
        if (!show)
        {
            block.SetActive(false);
            Transform model = transform.FindChild("Model");
            if (modelCreated)
            {
                DestroyImmediate(model.gameObject);
                modelCreated = false;
            }
        }
        if (show && blockShownInfo != id)
        {
            blockShownInfo = id;

            if (id < Block.prototypes.Length && Block.prototypes[id] != null)
            {
                // use cube
                if (modelCreated)
                {
                    DestroyImmediate(transform.FindChild("Model").gameObject);
                    modelCreated = false;
                }
                block.SetActive(true);
                block.GetComponent<MeshRenderer>().material.mainTexture = Inventory.getItemTexture(id);
            }
            else
            {
                // use item model
                if (modelCreated)
                {
                    DestroyImmediate(transform.FindChild("Model").gameObject);
                    // just preserve that modelCreated value, no need to change
                }
                GameObject prefab = (GameObject)Resources.Load("Entities/Items/" + id);
                GameObject obj = GameObject.Instantiate(prefab, transform, false);
                obj.transform.name = "Model";
                block.SetActive(false);
                modelCreated = true;
            }
        }
    }

    public void SetArmShown(bool show)
    {
        GameObject arm = hand.transform.FindChild("Arm").gameObject;
        if (arm.activeSelf == show) return;
        arm.SetActive(show);
    }

    public void updateFromMetadata(int elem, SerializedMetadata meta)
    {
        SerializedItem[] reference;
        switch (elem)
        {
            case 0:
                reference = items;
                break;
            case 1:
                reference = craftingInput;
                break;
            case 2:
                reference = craftingOutput;
                break;
            default:
                return;
        }
        if (meta.Entries.Count != reference.Length)
        {
            reference = new SerializedItem[meta.Entries.Count];
        }
        Debug.Log("UPDATED INVENTORY SIZE = " + items.Length);
        for (uint i = 0; i < meta.Entries.Count; i++)
        {
            reference[i] = new SerializedItem();
            reference[i].Id = meta.Entries[i].MetaValue.Entries[0].Int32Value;
            reference[i].Count = (uint)meta.Entries[i].MetaValue.Entries[1].Int32Value;
            reference[i].BinaryMeta = meta.Entries[i].MetaValue.Entries[2].MetaValue;
        }
        switch (elem)
        {
            case 0:
                items = reference;
                break;
            case 1:
                craftingInput = reference;
                break;
            case 2:
                craftingOutput = reference;
                break;
        }

        PlayerInventoryWindow invWindow = (PlayerInventoryWindow)WindowManager.INSTANCE.getInventoryWindow();
        if(invWindow != null)
        {
            switch (elem)
            {
                case 0:
                    invWindow.UpdateItems();
                    break;
                case 1:
                    invWindow.UpdateCraftingInputs();
                    break;
                case 2:
                    invWindow.UpdateCraftingOutput();
                    break;
            }
        }
    }
}

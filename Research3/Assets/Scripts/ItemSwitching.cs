using UnityEngine;

public class ItemSwitching : MonoBehaviour
{
    private int selectedItem = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int previousSelectedItem = selectedItem;


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedItem = 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedItem = 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedItem = 2;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedItem = 3;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            selectedItem = 4;
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            selectedItem = 5;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            selectedItem = 0;
        }

        if (previousSelectedItem != selectedItem)
        {
            SelectItem();
        }
    }


    void SelectItem()
    {
        int i = 0;

        foreach (Transform _item in transform)
        {
            if (i == selectedItem)
            {
                _item.gameObject.SetActive(true);
            }
            else
            {
                _item.gameObject.SetActive(false);
            }

            i++;
        }
    }
}

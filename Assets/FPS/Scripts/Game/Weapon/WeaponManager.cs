using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject[] weapons;
    public int startingWeaponIndex = 1;
    private int weaponIndex;

    void Start()
    {
        weaponIndex = startingWeaponIndex;
        SetActiveWeapon(weaponIndex);
    }

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Alpha1))
			SetActiveWeapon(0);
		if (Input.GetKeyDown(KeyCode.Alpha2))
			SetActiveWeapon(1);
		if (Input.GetKeyDown(KeyCode.Alpha3))
			SetActiveWeapon(2);
		if (Input.GetKeyDown(KeyCode.Alpha4))
			SetActiveWeapon(3);

		if (Input.GetAxis("Mouse ScrollWheel") > 0)
			NextWeapon();
		if (Input.GetAxis("Mouse ScrollWheel") < 0)
			PreviousWeapon();
	}

	public void SetActiveWeapon(int index)
	{
		if (index >= weapons.Length || index < 0)
		{
			Debug.LogWarning("ÎÞ¸ÃÎäÆ÷");
			return;
		}
		weaponIndex = index;
		for (int i = 0; i < weapons.Length; i++)
		{
			weapons[i].SetActive(false);
		}
		weapons[index].SetActive(true);
	}

	public void NextWeapon()
	{
		weaponIndex++;
		if (weaponIndex > weapons.Length - 1)
			weaponIndex = 0;
		SetActiveWeapon(weaponIndex);
	}

	public void PreviousWeapon()
	{
		weaponIndex--;
		if (weaponIndex < 0)
			weaponIndex = weapons.Length - 1;
		SetActiveWeapon(weaponIndex);
	}
}

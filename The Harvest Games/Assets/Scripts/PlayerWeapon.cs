using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerWeapon : MonoBehaviour
{
    public int selectedWeapon = 0;
    public Gun activeWeapon;
    public Transform bulletSpawnPos;
    public PlayerController player;
    void Awake()
    {
        // Ensure player is assigned properly
        player = GetComponent<PlayerController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        SelectWeapon(0);
    }

    public void TryShoot()
    {
        // can we shoot?
        if (activeWeapon.curAmmo <= 0 || Time.time - activeWeapon.lastShootTime < activeWeapon.shootRate)
            return;
        activeWeapon.curAmmo--;
        activeWeapon.lastShootTime = Time.time;
        // update the ammo UI
        GameUI.instance.UpdateAmmoText();
        // spawn the bullet
        player.photonView.RPC("SpawnBullet", RpcTarget.All, bulletSpawnPos.transform.position, Camera.main.transform.forward);
    }

    [PunRPC]
    void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        // spawn and orientate it
        GameObject bulletObj = Instantiate(activeWeapon.bulletPrefab, pos, Quaternion.identity);
        bulletObj.transform.forward = dir;

        // get bullet script
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();
        // initialize it and set the velocity
        bulletScript.Initialize(activeWeapon.damage, player.id, player.photonView.IsMine);
        bulletScript.rig.velocity = dir * activeWeapon.bulletSpeed;
    }

    [PunRPC]
    public void GiveAmmo(int ammoToGive)
    {
        activeWeapon.curAmmo = Mathf.Clamp(activeWeapon.curAmmo + ammoToGive, 0, activeWeapon.maxAmmo);
        // update the ammo text
        GameUI.instance.UpdateAmmoText();
    }

    public void SelectWeapon(int weaponToSelect)
    {
        player.photonView.RPC("SwitchWeapon", RpcTarget.All, weaponToSelect);
    }

    [PunRPC]
    void SwitchWeapon(int weaponToSwitch)
    {
        int i = 0;
        foreach (Transform weapon in player.weaponManager.transform)
        {
            if (i == weaponToSwitch)
            {
                weapon.gameObject.SetActive(true);
                Debug.Log(weapon.name);
                activeWeapon = weapon.gameObject.GetComponent<Gun>();
            }
            else
            {
                weapon.gameObject.SetActive(false);
            }
            i++;
        }
    }
}

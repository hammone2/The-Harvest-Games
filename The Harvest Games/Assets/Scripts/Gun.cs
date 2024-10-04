using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Stats")]
    public int damage;
    public int curAmmo;
    public int maxAmmo;
    public int zoom;
    public float zoomSpeed;
    public float bulletSpeed;
    public float shootRate;
    public float lastShootTime;
    public GameObject bulletPrefab;

    [Header("Equip Coords")]
    public float _x;
    public float _y;
    public float _z;
}

using UnityEngine;
public class DestroyEffect : MonoBehaviour
{
    // Bu kod, atıldığı objeyi (toz bulutunu) 0.5 saniye sonra sahneden tamamen siler.
    void Start() { Destroy(gameObject, 0.2f); }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    // Start is called before the first frame update
    public float lifeTime = 1.5f; // 이 오브젝트가 살아있을 시간 (Inspector에서 애니메이션 길이에 맞춰 설정!)

    public Vector2 damageBoxsize = new Vector2(2f, 2f);
    public Vector2 damageBoxOffset = new Vector2(0f, -0.5f);


    void Start()
    {
        // 이 오브젝트가 생성된 후 'lifeTime' 초 뒤에 스스로 파괴되도록 함
        Destroy(gameObject, lifeTime);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + (Vector3)damageBoxOffset, damageBoxsize);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splitting : MonoBehaviour
{
	private GameObject player;
	public GameObject smaller;

	public float speed;

	public int target;
	
	private float distance;
    // Start is called before the first frame update
    void Start()
    {
   	target = 25;    

	player = GameObject.Find("Player");

	if (gameObject.tag == "Small")
		smaller = null;
		
    }

    // Update is called once per frame
    void FixedUpdate()
    {
	
      distance = Vector2.Distance(transform.position, player.transform.position);
	
      if (gameObject.tag == "Big") {
      	if (distance > target)
	     Split();
      	else 
	     Chase();
      } //else if (gameObject.tag == "Small") {
              //	player = GameObject.Find("Player");
         //    Chase();
      //}	
    }

    void Chase()
    {
	target = 25;

	Vector2 direction = player.transform.position - transform.position;
	direction.Normalize();

	float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

	transform.position = Vector2.MoveTowards(this.transform.position, player.transform.position, speed * Time.deltaTime);
	transform.rotation = Quaternion.Euler(Vector3.forward * angle);
    }

    void Split()
    {
	//target = 2;

	Destroy(this.gameObject);
	
	Vector3 offset = new Vector3(1, 1, 0);
	Instantiate(smaller, transform.position + offset, transform.rotation);
	Instantiate(smaller, transform.position, transform.rotation);
    }
}

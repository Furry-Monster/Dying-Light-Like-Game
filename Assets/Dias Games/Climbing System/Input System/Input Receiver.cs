using UnityEngine;
using UnityEngine.InputSystem;

public class InputReceiver : MonoBehaviour
{
    //properties
    public Vector2 Move
    {
        get { return move; }
    }

    public Vector2 Look
    {
        get { return look; }
    }

    public bool Jump
    {
        get { return jump; }
    }
    public bool Walk
    {
        get { return walk; }
    }
    public bool Roll
    {
        get { return roll; }
    }
    public bool Crouch
    {
        get { return crouch; }
    }
    public bool Interact
    {
        get { return interact; }
    }
    public bool Crawl
    {
        get { return crawl; }
    }
    public bool Zoom
    {
        get { return zoom; }
    }
    public bool Drop
    {
        get { return drop; }
    }

    [Header("Cache Input")]
    private Vector2 move = Vector2.zero;
    private Vector2 look = Vector2.zero;
    private bool jump = false;
    private bool walk = false;
    private bool roll = false;
    private bool crouch = false;
    private bool interact = false;
    private bool crawl = false;
    private bool zoom = false;
    private bool drop = false;

    public void ResetActions()
    {
        jump = false;
        roll = false;
        crawl = false;
        interact = false;
        drop = false;
    }

    public void LegacyInput()
    {
        move.x = Input.GetAxis("Horizontal");
        move.y = Input.GetAxis("Vertical");

        look.x = Input.GetAxis("Mouse X");
        look.y = Input.GetAxis("Mouse Y");

        walk = Input.GetButton("Walk");
        jump = Input.GetButtonDown("Jump");
        roll = Input.GetButtonDown("Roll");
        crouch = Input.GetButton("Crouch");
        crawl = Input.GetButtonDown("Crawl");
        zoom = Input.GetButtonDown("Zoom");
        interact = Input.GetButtonDown("Interact");

        // special actions for climbing
        drop = Input.GetButtonDown("Drop");

        /*
        // special actions for shooter
        Fire = Input.GetButton("Fire");
        Reload = Input.GetButtonDown("Reload");
        Switch = Input.GetAxisRaw("Switch");
        Toggle = Input.GetButtonDown("Toggle");*/
    }

    public void OnMove(Vector2 value)
    {
        move = value;
    }
    public void OnLook(Vector2 value)
    {
        look = value;
    }
    public void OnJump(bool value)
    {
        jump = value;
    }
    public void OnWalk(bool value)
    {
        walk = value;
    }
    public void OnRoll(bool value)
    {
        roll = value;
    }
    public void OnCrouch(bool value)
    {
        crouch = value;
    }
    public void OnCrawl(bool value)
    {
        crawl = value;
    }

    public void OnZoom(bool value)
    {
        zoom = value;
    }
    public void OnInteract(bool value)
    {
        interact = value;
    }
    public void OnDrop(bool value)
    {
        drop = value;
    }

#if ENABLE_INPUT_SYSTEM
    private void OnMove(InputValue value)
    {
        OnMove(value.Get<Vector2>());
    }


    private void OnLook(InputValue value)
    {
        OnLook(value.Get<Vector2>());
    }

    private void OnJump(InputValue value)
    {
        OnJump(value.isPressed);
    }

    private void OnWalk(InputValue value)
    {
        OnWalk(value.isPressed);
    }

    private void OnRoll(InputValue value)
    {
        OnRoll(value.isPressed);
    }

    private void OnCrouch(InputValue value)
    {
        OnCrouch(value.isPressed);
    }


    private void OnCrawl(InputValue value)
    {
        OnCrawl(value.isPressed);
    }

    private void OnZoom(InputValue value)
    {
        OnZoom(value.isPressed);
    }

    private void OnInteract(InputValue value)
    {
        OnInteract(value.isPressed);
    }
    private void OnDrop(InputValue value)
    {
        OnDrop(value.isPressed);
    }

#endif
}

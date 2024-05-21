using UnityEngine;

namespace DiasGames.Abilities
{
    public class CharacterActions
    {
        public Vector2 move = Vector2.zero;

        public bool jump = false;
        public bool walk = false;
        public bool roll = false;
        public bool crouch = false;
        public bool drop = false;
        public bool crawl = false;
        public bool interact = false;

        // weapon actions
        public bool zoom = false;
        public bool fire = false;
        public bool reload = false;
        public bool toggle = false;
        public float switchWeapon = 0;
    }
}
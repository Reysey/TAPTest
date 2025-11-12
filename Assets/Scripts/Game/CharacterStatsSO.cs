using UnityEngine;

namespace Game
{
    public abstract class CharacterStatsSO : ScriptableObject
    {
        [Header("Stats")]
        public int maxHP = 10;
        public int attack = 2;
        public int defense = 1;
    }
}
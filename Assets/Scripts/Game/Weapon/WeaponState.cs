namespace Core.Weapon
{
    public enum WeaponState
    {
        BEGIN_SHOOTING,
        FAIL_SHOOTING,
        END_SHOOTING,
        END_SHOOTING_EMPTY,
        BEGIN_RELOADING,
        BEGIN_RELOADING_EMPTY,
        FAIL_RELOADING,
        INSERT_RELOADING,
        END_RELOADING,
        END_RELOADING_CHAMBERED,
        BEGIN_OPEN_BOLT,
        END_OPEN_BOLT,
        BEGIN_CLOSE_BOLT,
        END_CLOSE_BOLT,
        BEGIN_INSERT,
        END_INSERT,
    }
}
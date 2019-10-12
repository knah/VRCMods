using System;
using System.Linq;
using VRC;
using VRC.Core;

namespace JoinNotifier
{
    public static class PlayerReflection
    {
        private static Func<Player, APIUser> ourGetterFunc;
        static PlayerReflection()
        {
            ourGetterFunc = (Func<Player, APIUser>) Delegate.CreateDelegate(typeof(Func<Player, APIUser>),
                typeof(Player).GetProperties().Single(it => it.PropertyType == typeof(APIUser)).GetGetMethod());
        }

        public static APIUser GetApiUser(this Player player)
        {
            return ourGetterFunc(player);
        }
    }
}
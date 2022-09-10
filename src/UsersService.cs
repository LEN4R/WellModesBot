using LiteDB;
using System.IO;

namespace WellModesBot
{
    public class UsersService
    {
        private readonly LiteDatabase _database;

        public UsersService()
        {
            _database = new LiteDatabase("UsersService.db");
        }

        private class User
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public UserRole Role { get; set; }
        }

        public bool RegisterNewUser(long userId, string name = null, UserRole role = UserRole.@default)
        {
            var usersCollection = _database.GetCollection<User>("users");

            var userFromDB = usersCollection.FindById(new BsonValue(userId));

            if (userFromDB != null)
                return false;

            usersCollection.Insert(new User
            {
                Id = userId,
                Name = name,
                Role = role
            });

            return true;
        }
    }
}

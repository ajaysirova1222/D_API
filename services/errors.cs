using System.Collections.Generic;

public static class errors{

        public static Dictionary<int, string> err = new Dictionary<int, string>()
        {
            {100,"Database Connectivity Error"},
            {101,"Invalid Login Credentials"},
            {102,"hash error"},
            {103,"DATA NOT FOUND"},
            {104,"Mobile no Already Inserted"},
            {105,"Some details not found. Please check your payload."}
        };
}
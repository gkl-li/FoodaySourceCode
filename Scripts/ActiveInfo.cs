using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInfo : MonoBehaviour
{
    public enum Location { FRIDGE, CUSTOM_LOCATION }

    public static Storage activeStorage;
    //public static string apiKey = "ba11a6eda0ba441381fbc5506ec1b29f";
    //public static string apiKey = "b2daff5dac3f45d48d1555b7ef98961e";
    //public static string apiKey = "b68b30c236164655a263a9316b06867b";
    //public static string apiKey = "67a1dd997d1348cfa69fc1b7c7462131";
    public static string apiKey = "329206abd3514daaa0bb4aa943248bdc";
    public static User activeUser;
    public static List<RecipeAPIItem> savedRecipes = new List<RecipeAPIItem>();
    public static System.TimeSpan expirationRange = new System.TimeSpan(1, 0, 0, 0);

    [System.Serializable]
    public class Family
    {
        public int id;
        public List<User> users;
        public List<int> userIds;
        public Storage storage;

        public static Family GetNewFamily(int familyOwner)
        {
            Family ret = new Family();
            ret.id = int.Parse(DBManager.Select("CAST(COUNT(*) AS CHAR)", "Family", "")[0]) + 1; ;
            ret.users = new List<User>();
            ret.userIds = new List<int>();
            ret.userIds.Add(familyOwner);
            ret.storage = new Storage();
            ret.storage.items = new List<Item>();
            DBManager.UpdateDB(string.Format("INSERT INTO Family (ID,JSON) VALUES ({0},'{1}');", ret.id, Newtonsoft.Json.JsonConvert.SerializeObject(ret)));
            return ret;
        }

        public static Family GetFamilyFromDB(int familyId)
        {
            var jsons = DBManager.Select("JSON", "Family", " ID= " + familyId);
            if (jsons.Count == 0)
                return Family.GetNewFamily(ActiveInfo.activeUser.id);
            var fam = Newtonsoft.Json.JsonConvert.DeserializeObject<Family>(jsons[0]);
            fam.users = new List<User>();
            foreach (var v in fam.userIds)
                fam.users.Add(User.GetUserFromDB(v));
            return fam;
        }
    }


    [System.Serializable]
    public class User
    {
        public enum Allergens { SOY,FISH,MEAT,EGG }

        public int id;
        public string username;
        public int age;
        public List<Allergens> allergens;
        public int familyID;
        public int expiryNotificationDays = 1;

        public static User GetNewUser()
        {
            User ret = new User();
            ret.id = int.Parse(DBManager.Select("CAST(COUNT(*) AS CHAR)", "User","")[0])+1;
            ret.username = "NewUser" + ret.id;
            ret.age = 18;
            ret.allergens = new List<Allergens>();
            ret.familyID = -1;
            ret.expiryNotificationDays = 1;
            DBManager.UpdateDB(string.Format("INSERT INTO User (ID,JSON) VALUES ({0},'{1}');", ret.id, Newtonsoft.Json.JsonConvert.SerializeObject(ret)));
            return ret;
        }

        public string FormatAllergens()
        {
            var ret = "";
            try
            {
                foreach (var v in allergens)
                    ret += (v.ToString() + ",");
                ret = ret.Substring(0, ret.Length - 1);
            }
            catch
            {
                ret = "No allergies";
            }
            return ret;
        }

        public static User GetUserFromDB(int userId)
        {
            var jsons = DBManager.Select("JSON", "User", " ID= " + userId);
            if (jsons.Count == 0)
                return new User();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<User>(jsons[0]);
        }
    }

    public class Item
    {
        public string itemName;
        public System.DateTime purchaseDate;
        public System.DateTime expiryDate;
        public string purchasedBy;
        public float totalQuantity;
        public string unit;
        public Location location;
        public List<System.Tuple<System.DateTime, string, float>> consumptionHistory;
        public bool isArchived = false;
    }
    public class Storage
    {
        public Storage()
        {
            items = new List<Item>();
        }
        public List<Item> items;

        public void SortByArchived()
        {
            items.Sort((x, y) => (y.isArchived ? 0 : 1) - (x.isArchived ? 0 : 1));
        }
    }
    [System.Serializable]

    public class IngredientAPIResult
    {
        public List<IngredientAPIItem> results;
        public int offset;
        public int number;
        public int totalResults;
    }

    [System.Serializable]
    public class IngredientAPIItem
    {
        public int id;
        public string name;
        public string image;
    }
    [System.Serializable]

    public class RecipeAPIResult
    {
        public List<RecipeAPIItem> results;
    }

    [System.Serializable]
    public class RecipeAPIItem
    {
        public int id;
        public string title;
        public string image;
        public string imageType;
        public int usedIngredientCount;
        public int missedIngredientCount;
        public List<RecipeAPIIngredientItem> missedIngredients;
        public List<RecipeAPIIngredientItem> usedIngredients;
        public List<RecipeAPIIngredientItem> unusedIngredients;
        public int likes;
    }


    [System.Serializable]
    public class RecipeAPIIngredientItem
    {
        public int id;
        public float amount;
        public string unit;
        public string unitLong;
        public string unitShort;
        public string aisle;
        public string name;
        public string original;
        public string originalString;
        public string originalName;
        public List<string> metaInformation;
        public List<string> meta;
        public string image;
    }

    [System.Serializable]
    public class RecipeInstructionAPIResultWrapper
    {
        public RecipeInstructionAPIResult result;
        public string GetAllSteps()
        {
            var ret = "";
            foreach (var v in result.steps)
                ret += (v.step+"\n");
            return ret;
        }
    }


    [System.Serializable]
    public class RecipeInstructionAPIResult
    {
        public string name;
        public List<RecipeInstructionAPISteps> steps;
    }

    [System.Serializable]
    public class RecipeInstructionAPISteps
    {
        public List<RecipeInstructionAPIEquipment> equipment;
        public List<RecipeInstructionAPIIngredient> ingredients;
        public int number;
        public string step;
    }

    [System.Serializable]
    public class RecipeInstructionAPIEquipment
    {
        public int id;
        public string image;
        public string name;
        public RecipeInstructionAPITemperature temperature;
    }

    [System.Serializable]
    public class RecipeInstructionAPITemperature
    {
        public float number;
        public string unit;
    }

    [System.Serializable]
    public class RecipeInstructionAPIIngredient
    {
        public int id;
        public string image;
        public string name;
    }

    public class UserInfo
    {
        public string username;
    }

    public class MockInfo
    {
        public static Storage GetMockStorage1()
        {
            Storage ret = new Storage();

            var i1 = new Item
            {
                itemName = "Potato",
                purchaseDate = System.DateTime.Now,
                expiryDate = System.DateTime.Now + new System.TimeSpan(12, 0, 0, 0),
                purchasedBy = "Steven",
                totalQuantity = 5,
                unit = "kg",
                location = Location.FRIDGE,
                consumptionHistory = new List<System.Tuple<System.DateTime, string, float>>()
            };
            i1.consumptionHistory.Add(new System.Tuple<System.DateTime, string, float>(
                    System.DateTime.Today - new System.TimeSpan(0,0,0,1),
                    "Charles",
                    1.5f));
            i1.consumptionHistory.Add(new System.Tuple<System.DateTime, string, float>(
                    System.DateTime.Today,
                    "Hugo",
                    1.15f));
            var i2 = new Item
            {
                itemName = "Lettuce",
                purchaseDate = System.DateTime.Now,
                expiryDate = System.DateTime.Now + new System.TimeSpan(5, 0, 0, 0),
                purchasedBy = "Jarrold",
                totalQuantity = 2.5f,
                unit = "kg",
                location = Location.FRIDGE,
                consumptionHistory = new List<System.Tuple<System.DateTime, string, float>>()
            };
            i2.consumptionHistory.Add(new System.Tuple<System.DateTime, string, float>(
                    System.DateTime.Today,
                    "Patterson",
                    0.3f));

            ret.items.Add(i1);
            ret.items.Add(i2);
            
            return ret;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Net.Http;
using UnityEditor;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class MainManager
    {
        public List<GameObject> existingObjects;
        public GameObject template;
        public VerticalLayoutGroup itemParent;
        public GameObject detailsObject;
        public GameObject mainObject;
        public GameObject manualInputObject;
        public GameObject scannerObject;
        public int ingredientsPageCount = 0;
    }

    [System.Serializable]
    public class DetailsManager
    {
        public List<Text> refTexts;
        public Slider consumptionSlider;
        public Button consumptionButton;
        public RawImage itemRawImage;
        public Button archiveButton;
    }

    [System.Serializable]
    public class RecipesManager
    {
        public static string formatUrl = "https://api.spoonacular.com/recipes/findByIngredients?ingredients={0}&number=15&apiKey={1}";
        public List<GameObject> existingObjects;
        public GameObject template;
        public VerticalLayoutGroup itemParent;
        public GameObject detailsObject;
        public GameObject mainObject;
        public Button recommendButton;
        public Button savedButton;
        public int cookbookPageCount = 0;
        public ActiveInfo.RecipeAPIResult activeResult;
    }

    [System.Serializable]
    public class AccountManager
    {
        public Text usernameText;
        public InputField usernameInputField;

        public Slider ageSlider;
        public Text ageText;
        public Text ageSliderText; 
        
        public Slider notifyDateSlider;

        public List<Button> allergyButtons;
        public List<Button> editPanelAllergyButtons;

        public GameObject template;
        public List<GameObject> existingObjects;
        public VerticalLayoutGroup itemParent;

        public GameObject mainPage;
        public GameObject editPage;
        public GameObject familyPage;
        public GameObject settingsPage;
    }

    public AccountManager accountManager = new AccountManager();

    public void LoadFamilyPage()
    {
        var familyID = ActiveInfo.activeUser.familyID;
        var family = ActiveInfo.Family.GetFamilyFromDB(familyID);
        List<ActiveInfo.User> users = new List<ActiveInfo.User>();
        if (accountManager.existingObjects == null) accountManager.existingObjects = new List<GameObject>();
        foreach (var v in accountManager.existingObjects)
            Destroy(v);
        print(family);
        print(family.users);
        foreach (var v in family.users)
        {
            var tmp = Instantiate(accountManager.template, accountManager.itemParent.transform);
            accountManager.existingObjects.Add(tmp);
            tmp.SetActive(true);
            var text = tmp.GetComponentInChildren<Text>();
            text.text = string.Format("{0}/{1}/{2}", v.username, v.age, v.FormatAllergens());
        }
    }

    public void UploadFamilyStorageStatus()
    {
        var famID = ActiveInfo.activeUser.familyID;
        var fam = ActiveInfo.Family.GetFamilyFromDB(famID);
        fam.storage.items = new List<ActiveInfo.Item>(ActiveInfo.activeStorage.items);
        print(fam.storage.items.Count);
        DBManager.UpdateDB(string.Format("UPDATE Family SET JSON = '{0}' WHERE ID = {1};", Newtonsoft.Json.JsonConvert.SerializeObject(fam),famID));
    }

    public void LoadAccountsPage()
    {
        var u = ActiveInfo.activeUser;
        accountManager.ageSlider.value = u.age;
        accountManager.ageSlider.onValueChanged.RemoveAllListeners();
        accountManager.ageSlider.onValueChanged.AddListener(delegate
        {
            accountManager.ageSliderText.text = accountManager.ageSlider.value + " years old";
        });
        accountManager.ageText.text = u.age + " years old";
        accountManager.usernameText.text = u.username;
        accountManager.usernameInputField.text = u.username;
        for (int i = 0; i < u.allergens.Count; i++)
        {
            accountManager.editPanelAllergyButtons[(int)u.allergens[i]].image.color = Color.green;
            accountManager.allergyButtons[(int)u.allergens[i]].image.color = Color.green;
        }
    }
    public void ChangeButtonColor(Button button)
    {
        if (button.image.color != Color.green)
            button.image.color = Color.green;
        else
            button.image.color = Color.gray;
    }

    public void CreateNewFamily()
    {
        var oldFamJsons = DBManager.Select("JSON", "Family", "ID=" + ActiveInfo.activeUser.familyID);
        if (oldFamJsons.Count > 0)
        {
            var oldFam = Newtonsoft.Json.JsonConvert.DeserializeObject<ActiveInfo.Family>(oldFamJsons[0]);
            oldFam.userIds.Remove(ActiveInfo.activeUser.id);
            DBManager.UpdateDB(string.Format("UPDATE Family SET JSON = '{0}' WHERE ID = {1};",
                Newtonsoft.Json.JsonConvert.SerializeObject(oldFam),
                oldFam.id));
        }
        var fam = ActiveInfo.Family.GetNewFamily(ActiveInfo.activeUser.id);
        var storage = fam.storage;
        ActiveInfo.activeUser.familyID = fam.id;
        DBManager.UpdateDB(string.Format("UPDATE User SET JSON = '{0}' WHERE ID = {1};",
                    Newtonsoft.Json.JsonConvert.SerializeObject(ActiveInfo.activeUser),
                    ActiveInfo.activeUser.id));
        Alert("You have created a new family!\nYou can share your QR code for others to join");
        LoadFamilyPage();
        StartCoroutine(SyncFromStorage());
    }

    public void JoinFamily(Button joiningButton)
    {
        StartCoroutine(W());
        IEnumerator W()
        {
            string codeMsg = "";
            yield return StartCoroutine(QRReader.ReadQRCode(joiningButton));
            if (alertObject.activeInHierarchy)
                yield break;
            try
            {
                codeMsg = QRReader.qrContent;
                if (codeMsg.Contains("FoodayJoinFamily") == false)
                {
                    Alert("This QR code is not from Fooday!");
                    yield break;
                }
            }
            catch
            {
                yield break;
            }
            
            int familyID = int.Parse(codeMsg.Split(':')[1]);
            var famJson = DBManager.Select("JSON", "Family", "ID=" + familyID)[0];
            var fam = Newtonsoft.Json.JsonConvert.DeserializeObject<ActiveInfo.Family>(famJson);
            if (fam.userIds.Contains(ActiveInfo.activeUser.id))
                Alert("You are already in this family");
            else
            {
                var oldFamJsons = DBManager.Select("JSON", "Family", "ID=" + ActiveInfo.activeUser.familyID);
                if(oldFamJsons.Count > 0)
                {
                    var oldFam = Newtonsoft.Json.JsonConvert.DeserializeObject<ActiveInfo.Family>(oldFamJsons[0]);
                    oldFam.userIds.Remove(ActiveInfo.activeUser.id);
                    DBManager.UpdateDB(string.Format("UPDATE Family SET JSON = '{0}' WHERE ID = {1};",
                        Newtonsoft.Json.JsonConvert.SerializeObject(oldFam),
                        oldFam.id));
                }
                fam.userIds.Add(ActiveInfo.activeUser.id);
                ActiveInfo.activeUser.familyID = familyID;
                DBManager.UpdateDB(string.Format("UPDATE User SET JSON = '{0}' WHERE ID = {1};", 
                    Newtonsoft.Json.JsonConvert.SerializeObject(ActiveInfo.activeUser),
                    ActiveInfo.activeUser.id));
                DBManager.UpdateDB(string.Format("UPDATE Family SET JSON = '{0}' WHERE ID = {1};", 
                    Newtonsoft.Json.JsonConvert.SerializeObject(fam),
                    familyID));
                Alert("You have joined Family " + familyID + " !\nPrevious family exited.");
                ActiveInfo.activeStorage = fam.storage;
                LoadFamilyPage();
            }
        }
    }


    public GameObject alertObject;
    public void Alert(string msg)
    {
        alertObject.SetActive(true);
        alertObject.GetComponentInChildren<Text>().text = msg;  
    }

    public void SaveUserDetails()
    {
        ActiveInfo.activeUser.age = (int)accountManager.ageSlider.value;
        ActiveInfo.activeUser.username = accountManager.usernameInputField.text;
        ActiveInfo.activeUser.allergens = new List<ActiveInfo.User.Allergens>();

        for (int i = 0; i < accountManager.allergyButtons.Count; i++)
        {
            accountManager.allergyButtons[i].image.color = accountManager.editPanelAllergyButtons[i].image.color;
        }
        for (int i = 0; i < accountManager.editPanelAllergyButtons.Count; i++)
        {
            int idx = i;
            if (accountManager.allergyButtons[i].image.color.Equals(Color.green))
                ActiveInfo.activeUser.allergens.Add((ActiveInfo.User.Allergens)idx);
        }
        foreach (var v in ActiveInfo.activeUser.allergens)
            print(v);
        DBManager.UpdateDB(string.Format("UPDATE User SET JSON = '{0}' WHERE ID = {1};",Newtonsoft.Json.JsonConvert.SerializeObject(ActiveInfo.activeUser),ActiveInfo.activeUser.id));
    }

    [System.Serializable]
    public class ManualInputManager
    {
        public InputField name;
        public InputField date;
        public InputField by;
        public InputField quantity;
        public InputField location;
        public InputField expiry;

        public ActiveInfo.Item ToItem()
        {
            ActiveInfo.Item item = new ActiveInfo.Item();
            item = new ActiveInfo.Item
            {
                itemName = name.text,
                purchaseDate = System.DateTime.Parse(date.text),
                expiryDate = System.DateTime.Parse(expiry.text),
                purchasedBy = by.text,
                totalQuantity = float.Parse(quantity.text),
                unit = "kg",
                location = location.text.ToLower() == "fridge" ? ActiveInfo.Location.FRIDGE : ActiveInfo.Location.CUSTOM_LOCATION,
                consumptionHistory = new List<System.Tuple<System.DateTime, string, float>>()
            };
            return item;
        }
    }

    public ManualInputManager manualInputManager;

    public void AddItem()
    {
        ActiveInfo.activeStorage.items.Add(manualInputManager.ToItem());
        UploadFamilyStorageStatus();
        ActiveInfo.activeStorage.SortByArchived();
    }


    [System.Serializable]

    public class PanelsManager
    {
        public GameObject mainPanel;
        public GameObject cookbookPanel;
        public GameObject accountPanel;
    }

    [System.Serializable]
    public class InstructionManager
    {
        public RawImage image;
        public Text titleText;
        public Text instructionText;
        public Button cookButton;
    }


    [System.Serializable]
    public class ScannedInputManager
    {
        public RawImage scanningImageShower;
        public Text outputResult;

        [System.Serializable]
        public class Entry
        {
            public string data;
            public float confidenceLevel;
        }

        [System.Serializable]
        public class ScannedResult
        {
            public Entry totalAmount;
            public Entry taxAmount;
            public Entry merchantName;
            public Entry merchantAddress;
            public Entry merchantCity;
            public Entry merchantState;
            public Entry merchantCountryCode;
            public Entry merchantPostalCode;
            public Entry merchantTypes;

            public List<System.Tuple<string,string>> GetEntries()
            {
                var ret = new List<System.Tuple<string, string>>();
                float threshold = 0.01f;
                if (totalAmount.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Total amount ", (string)totalAmount.data));
                if (taxAmount.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Tax amount ", (string)taxAmount.data));
                if (merchantName.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant name ", (string)merchantName.data));
                if (merchantAddress.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant address ", (string)merchantAddress.data));
                if (merchantCity.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant city ", (string)merchantCity.data));
                if (merchantState.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant state ", (string)merchantState.data));
                if (merchantCountryCode.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant country code ", (string)merchantCountryCode.data));
                if (merchantPostalCode.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant postal code ", (string)merchantPostalCode.data));
                if (merchantTypes.confidenceLevel > threshold)
                    ret.Add(new Tuple<string, string>("Merchant types ", (string)merchantTypes.data));
                return ret;
            }
        }
    }


    public MainManager mainManager = new MainManager();
    public DetailsManager detailsManager = new DetailsManager();
    public PanelsManager panelsManager = new PanelsManager();
    public RecipesManager recipesManager = new RecipesManager();
    public InstructionManager instructionManager = new InstructionManager();
    public ScannedInputManager scannedInputManager = new ScannedInputManager();

    public void ScanReceipt()
    {
        var fileName = Crosstales.FB.FileBrowser.Instance.OpenSingleFile("jpg");
        var taggunApiKey = "a2a183302c5c11ec8215c512ccf27e54";
        var taggunApiUrl = "https://api.taggun.io/api/receipt/v1/simple/file";
        if (!System.IO.File.Exists(fileName))
        {
            Alert("The file selected does not exist");
            return;
        }

        byte[] fileData = System.IO.File.ReadAllBytes(fileName);
        Texture2D moddedTexture = new Texture2D(
            (int)scannedInputManager.scanningImageShower.mainTexture.width,
            (int)scannedInputManager.scanningImageShower.mainTexture.height, TextureFormat.DXT1, false);
        moddedTexture.LoadImage(fileData);
        moddedTexture.wrapMode = TextureWrapMode.Clamp;
        moddedTexture.Apply(false, true);
        scannedInputManager.scanningImageShower.texture = moddedTexture;

        var timeStart = DateTime.Now;

        using (var httpClient = new HttpClient { Timeout = new TimeSpan(0, 0, 0, 60, 0) })
        {
            HttpResponseMessage response = null;

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("apikey", taggunApiKey);

            var parentContent = new MultipartFormDataContent("----WebKitFormBoundaryfzdR3Imh7urK8qw");

            var documentContent = new ByteArrayContent(fileData);
            documentContent.Headers.Remove("Content-Type");
            documentContent.Headers.Remove("Content-Disposition");
            documentContent.Headers.TryAddWithoutValidation("Content-Type", "image/jpeg");
            documentContent.Headers.TryAddWithoutValidation("Content-Disposition",
            string.Format(@"form-data; name=""file""; filename=""{0}""", "testfilename.jpg"));
            parentContent.Add(documentContent);

            var refreshContent = new StringContent("false");
            refreshContent.Headers.Remove("Content-Type");
            refreshContent.Headers.Remove("Content-Disposition");
            refreshContent.Headers.TryAddWithoutValidation("Content-Disposition", @"form-data; name=""refresh""");
            parentContent.Add(refreshContent);

            response = httpClient.PostAsync(taggunApiUrl, parentContent).Result;
            response.EnsureSuccessStatusCode();
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ScannedInputManager.ScannedResult>(response.Content.ReadAsStringAsync().Result);
            scannedInputManager.outputResult.text = ""; 

            foreach(var v in result.GetEntries())
            {
                scannedInputManager.outputResult.text += (v.Item1+":"+v.Item2+"\n");
            }
        }
    }


    public void CloseDetailsObject()
    {
        mainManager.mainObject.SetActive(false);
        mainManager.manualInputObject.SetActive(false);
        mainManager.detailsObject.SetActive(false);
        mainManager.scannerObject.SetActive(false);
        recipesManager.mainObject.SetActive(false);
        recipesManager.detailsObject.SetActive(false);
    }

    public void SetObjectActive(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }
    public void SetButtonInteractable(Button button)
    {
        button.interactable = (true);
    }
    public void SetButtonNotInteractable(Button button)
    {
        button.interactable = (false);
    }

    public void SetObjectInactive(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void NextCookbookPage()
    {
        if (recipesManager.cookbookPageCount >= 3)
            return;
        recipesManager.cookbookPageCount++;
        StartCoroutine(LoadCookbookPage());
    }

    public void PrevCookbookPage()
    {
        if (recipesManager.cookbookPageCount <= 0)
            return;
        recipesManager.cookbookPageCount--;
        StartCoroutine(LoadCookbookPage());
    }

    public void NextIngredientsPage()
    {
        if (mainManager.ingredientsPageCount >= ActiveInfo.activeStorage.items.Count / 5)
            return;
        mainManager.ingredientsPageCount++;
        StartCoroutine(SyncFromStorage());
    }

    public void PrevIngredientsPage()
    {
        if (mainManager.ingredientsPageCount <= 0)
            return;
        mainManager.ingredientsPageCount--;
        StartCoroutine(SyncFromStorage());
    }

    public void SyncUsernameToInputField(InputField inputField)
    {
        inputField.text = ActiveInfo.activeUser.username;
    }

    public IEnumerator LoadCookbookPage() 
    {
        int startIdx = recipesManager.cookbookPageCount * 4;
        var results = recipesManager.activeResult.results.GetRange(startIdx,Mathf.Min(4,recipesManager.activeResult.results.Count - startIdx));
        foreach (var v in recipesManager.existingObjects)
            Destroy(v);
        foreach (var v in results)
        {
            var tmp = Instantiate(recipesManager.template, recipesManager.itemParent.transform);
            recipesManager.existingObjects.Add(tmp);
            tmp.SetActive(true);
            var texts = tmp.GetComponentsInChildren<Text>();
            var images = tmp.GetComponentsInChildren<Image>();
            var buttons = tmp.GetComponentsInChildren<Button>();
            string imageLink = string.Format("https://spoonacular.com/recipeImages/{0}-240x150.{1}", v.id, v.imageType);
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageLink);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
                Debug.Log(request.error);
            else
                tmp.GetComponentInChildren<RawImage>().texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            texts[0].text = v.title;
            texts[1].text = "Ingredients: ";
            foreach (var i in v.usedIngredients)
                texts[1].text += (i.name + ",");
            texts[1].text = texts[1].text.Substring(0, texts[1].text.Length - 1);
            buttons[1].GetComponentInChildren<Text>().text = "Save";
            buttons[1].image.color = Color.white;
            foreach (var r in ActiveInfo.savedRecipes)
            {
                if (r.title == v.title)
                {
                    buttons[1].GetComponentInChildren<Text>().text = "Saved";
                    buttons[1].image.color = Color.gray;
                }
            }
            buttons[0].onClick.AddListener(delegate
            {
                IEnumerator W()
                {
                    var instructionLink = string.Format("https://api.spoonacular.com/recipes/{0}/analyzedInstructions?&apiKey={1}", v.id, ActiveInfo.apiKey);
                    ActiveInfo.RecipeInstructionAPIResultWrapper wrapper = new ActiveInfo.RecipeInstructionAPIResultWrapper();
                    yield return StartCoroutine(GetRequest(instructionLink, wrapper));
                    instructionManager.image.texture = tmp.GetComponentInChildren<RawImage>().texture;
                    instructionManager.titleText.text = v.title;
                    instructionManager.instructionText.text = wrapper.GetAllSteps();
                    recipesManager.detailsObject.SetActive(true);
                    var detailsButtons = recipesManager.detailsObject.GetComponentsInChildren<Button>(true);
                    var detailsTexts = recipesManager.detailsObject.GetComponentsInChildren<Text>(true);
                    detailsButtons[1].onClick.RemoveAllListeners();
                    detailsButtons[1].onClick.AddListener(delegate 
                    {
                        var str = "";
                        List<System.Tuple<ActiveInfo.Item, float>> toConsume = new List<Tuple<ActiveInfo.Item, float>>();
                        foreach(var ingredient in v.usedIngredients)
                        {
                            float convertedToKg = 0;
                            if (ingredient.unitShort.ToLower().Contains("cup"))
                            {
                                convertedToKg = 0.2f;
                            }
                            else if (ingredient.unitShort.ToLower().Contains("kg"))
                            {
                                convertedToKg = 1f;
                            }
                            else if (ingredient.unitShort.ToLower().Contains("pound"))
                            {
                                convertedToKg = 0.453f;
                            }
                            else if (ingredient.unitShort.ToLower().Contains("serving"))
                            {
                                convertedToKg = 0.2f;
                            }
                            else if (ingredient.unitShort.ToLower().Contains("ounce"))
                            {
                                convertedToKg = 0.028f;
                            }
                            else if (ingredient.unitShort.ToLower().Contains("lea"))
                            {
                                convertedToKg = 0.01f;
                            }
                            else
                            {
                                convertedToKg = 0.1f;
                            }
                            str += string.Format("{0} {1} {2}{3}\n", ingredient.name, ingredient.amount, ingredient.unitLong,
                                convertedToKg > 0.999?"":string.Format("({0} kg)",convertedToKg*ingredient.amount));
                            ActiveInfo.Item targetItem = null;
                            foreach(var item in ActiveInfo.activeStorage.items)
                            {
                                if (ingredient.name.ToLower().Contains(item.itemName.ToLower()))
                                {
                                    targetItem = item;
                                    break;
                                }
                            }
                            float remainder = targetItem.totalQuantity;
                            foreach (var h in targetItem.consumptionHistory)
                                remainder -= h.Item3;
                            toConsume.Add(new Tuple<ActiveInfo.Item, float>(targetItem, Mathf.Min(convertedToKg*ingredient.amount,remainder))); 
                        }
                        instructionManager.cookButton.onClick.RemoveAllListeners();
                        instructionManager.cookButton.onClick.AddListener(delegate
                        {
                            foreach(var c in toConsume)
                            {
                                c.Item1.consumptionHistory.Add(
                                new System.Tuple<System.DateTime, string, float>(
                                    System.DateTime.Now,
                                    ActiveInfo.activeUser.username,
                                    c.Item2));
                                var remainder = c.Item1.totalQuantity;
                                foreach (var h in c.Item1.consumptionHistory)
                                    remainder -= h.Item3;
                                if (remainder < 0.001)
                                {
                                    c.Item1.isArchived = true;
                                    ActiveInfo.activeStorage.SortByArchived();
                                    detailsManager.archiveButton.interactable = false;
                                    detailsManager.archiveButton.GetComponentInChildren<Text>().text = "Archived";
                                }
                            }
                            UploadFamilyStorageStatus();
                        });
                        detailsTexts[4].text = str;
                    });
                    
                    recipesManager.mainObject.SetActive(false);
                }
                StartCoroutine(W());
            });
            buttons[1].onClick.AddListener(delegate
            {
                var t = buttons[1].GetComponentInChildren<Text>();
                if (t.text == "Save")
                {
                    t.text = "Saved";
                    buttons[1].image.color = Color.gray;
                    ActiveInfo.savedRecipes.Add(v);
                }
                else
                {
                    t.text = "Save";
                    buttons[1].image.color = Color.white;
                    ActiveInfo.savedRecipes.Remove(v);
                }
            });
        }

    }

    public void LoadCookbook(bool isSavedRecipes)
    {
        StartCoroutine(Wrap());
        IEnumerator Wrap()
        {
            recipesManager.cookbookPageCount = 0;
            if (isSavedRecipes)
            {
                recipesManager.savedButton.interactable = false;
                recipesManager.recommendButton.interactable = true;
            }
            else
            {
                recipesManager.savedButton.interactable = true;
                recipesManager.recommendButton.interactable = false;
            }

            ActiveInfo.RecipeAPIResult result = new ActiveInfo.RecipeAPIResult();
            if (!isSavedRecipes)
            {               
                panelsManager.accountPanel.SetActive(false);
                panelsManager.mainPanel.SetActive(false);
                panelsManager.cookbookPanel.SetActive(true);

                
                var storageItemNames = "";
                foreach (var v in ActiveInfo.activeStorage.items)
                {
                    if(v.isArchived == false)
                        storageItemNames += (v.itemName + ",");
                }
                storageItemNames = storageItemNames.Substring(0, storageItemNames.Length - 1);
                yield return StartCoroutine(GetRequest(storageItemNames, result));
            }
            else
            {
                result.results = ActiveInfo.savedRecipes;
            }
            recipesManager.activeResult = result;
            yield return StartCoroutine(LoadCookbookPage());
        }
    }

    public void WrappedSyncFromStorage()
    {
        StartCoroutine(SyncFromStorage());
    }

    public IEnumerator SyncFromStorage()
    {
        yield return StartCoroutine(Wrapped());
        IEnumerator Wrapped()
        {
            var storage = ActiveInfo.activeStorage;
            storage.SortByArchived();

            foreach (var v in mainManager.existingObjects)
                Destroy(v);
            int maxCount = 5;
            int startIdx = mainManager.ingredientsPageCount * maxCount;
            var results = storage.items.GetRange(startIdx, Mathf.Min(maxCount, storage.items.Count - startIdx));
            print(startIdx);
            print(results.Count);

            List<ActiveInfo.Item> aboutToExpire = new List<ActiveInfo.Item>();

            foreach(var v in results)
            {
                if(v.expiryDate.Subtract(ActiveInfo.expirationRange)< System.DateTime.Now)
                {
                    if(v.isArchived == false)
                        aboutToExpire.Add(v);
                }
            }

            if(aboutToExpire.Count > 0)
            {
                string alertStr = "";
                for (int i = 0; i < 4 && i < aboutToExpire.Count; i++)
                {
                    alertStr += string.Format("{0} is about to/has expired (Bought@{1})", aboutToExpire[i].itemName, aboutToExpire[i].purchaseDate);
                }
                Alert(alertStr);
            }


            foreach (var v in results)
            {
                ActiveInfo.IngredientAPIResult result = new ActiveInfo.IngredientAPIResult();
                yield return StartCoroutine(GetRequest(v.itemName,result));
                var tmp = Instantiate(mainManager.template, mainManager.itemParent.transform);
                mainManager.existingObjects.Add(tmp);
                tmp.SetActive(true);
                var texts = tmp.GetComponentsInChildren<Text>();
                var images = tmp.GetComponentsInChildren<Image>();
                var buttons = tmp.GetComponentsInChildren<Button>();
                string imageLink = string.Format("https://spoonacular.com/cdn/ingredients_100x100/{0}", result.results[0].image);
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageLink);
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                    Debug.Log(request.error);
                else
                    tmp.GetComponentInChildren<RawImage>().texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                texts[0].text = v.itemName;
                float remainder = v.totalQuantity;
                foreach (var c in v.consumptionHistory)
                    remainder -= c.Item3;
                images[1].fillAmount = remainder / v.totalQuantity;
                texts[2].text = (int)(100 * images[1].fillAmount) + "%";
                texts[3].text = remainder + "/" + v.totalQuantity + v.unit;
                var freshnessAllowed = v.expiryDate;
                var freshnessPercentage = (float)(v.expiryDate.Subtract(System.DateTime.Today).TotalDays / v.expiryDate.Subtract(v.purchaseDate).TotalDays);
                freshnessPercentage = Mathf.Max(0, freshnessPercentage);
                images[3].fillAmount = freshnessPercentage;
                texts[5].text = (int)(100 * freshnessPercentage) + "%";
                texts[6].text = 
                    freshnessPercentage < 0.1 
                    ? "Expired"
                    : v.expiryDate.Subtract(System.DateTime.Today).Days + "days until expire";
                if(remainder < 0.01 || freshnessPercentage < 0.1 || v.isArchived)
                {
                    buttons[1].image.color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.5f);
                    v.isArchived = true;
                    ActiveInfo.activeStorage.SortByArchived();
                    detailsManager.archiveButton.interactable = false;
                    detailsManager.archiveButton.GetComponentInChildren<Text>().text = "Archived";
                }
                if (v.location != ActiveInfo.Location.FRIDGE)
                    buttons[0].image.color = Color.white;
                buttons[1].onClick.AddListener(delegate
                {
                    if (v.isArchived)
                    {
                        detailsManager.archiveButton.interactable = false;
                        detailsManager.archiveButton.GetComponentInChildren<Text>().text = "Archived";
                    }
                    else
                    {
                        detailsManager.archiveButton.interactable = true;
                        detailsManager.archiveButton.GetComponentInChildren<Text>().text = "Archive";
                    }
                    UploadFamilyStorageStatus();
                    mainManager.mainObject.SetActive(false);
                    mainManager.detailsObject.SetActive(true);
                    detailsManager.itemRawImage.texture = tmp.GetComponentInChildren<RawImage>().texture;
                    detailsManager.refTexts[7].text = v.itemName;
                    detailsManager.refTexts[0].text = v.purchaseDate.ToShortDateString();
                    detailsManager.refTexts[1].text = v.purchasedBy;
                    detailsManager.refTexts[2].text = v.totalQuantity + v.unit;
                    detailsManager.refTexts[3].text = v.location.ToString();
                    detailsManager.refTexts[4].text = v.expiryDate.ToShortDateString();
                    string history = "";
                    foreach (var h in v.consumptionHistory)
                    {
                        history += string.Format("{0} {1} consumed {2}{3}\n", h.Item1.ToShortDateString(), h.Item2, h.Item3, v.unit);
                    }
                    detailsManager.refTexts[5].text = history;
                    detailsManager.consumptionSlider.onValueChanged.RemoveAllListeners();
                    detailsManager.consumptionSlider.onValueChanged.AddListener(
                        delegate {
                            detailsManager.refTexts[6].text = detailsManager.consumptionSlider.value * remainder + "kg";
                        });
                    float tmpRemainder = remainder;
                    detailsManager.consumptionButton.onClick.RemoveAllListeners();
                    detailsManager.consumptionButton.onClick.AddListener(
                        delegate {
                            if (detailsManager.consumptionSlider.value * remainder < 0.001f)
                                return;
                            v.consumptionHistory.Add(
                                new System.Tuple<System.DateTime, string, float>(
                                    System.DateTime.Now,
                                    ActiveInfo.activeUser.username,
                                    detailsManager.consumptionSlider.value * remainder));
                            history = "";
                            foreach (var h in v.consumptionHistory)
                            {
                                history += string.Format("{0} {1} consumed {2}{3}\n", h.Item1.ToShortDateString(), h.Item2, h.Item3, v.unit);
                            }
                            detailsManager.refTexts[5].text = history;
                            remainder = v.totalQuantity;
                            foreach (var c in v.consumptionHistory)
                                remainder -= c.Item3;
                            images[1].fillAmount = remainder / v.totalQuantity;
                            texts[2].text = (int)(100 * images[1].fillAmount) + "%";
                            texts[3].text = remainder + "/" + v.totalQuantity + v.unit;
                            if (remainder < 0.01 || freshnessPercentage < 0.1)
                            {
                                buttons[1].image.color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.5f);
                                v.isArchived = true;
                                ActiveInfo.activeStorage.SortByArchived();
                                detailsManager.archiveButton.interactable = false;
                                detailsManager.archiveButton.GetComponentInChildren<Text>().text = "Archived";
                            }
                            UploadFamilyStorageStatus();
                        }
                        );
                });
            }
        }

    }

    IEnumerator GetRequest(string url,ActiveInfo.RecipeInstructionAPIResultWrapper resultWrapper)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = url.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                var json = webRequest.downloadHandler.text;
                Debug.Log(pages[page] + ":\nReceived: " + json);
                resultWrapper.result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActiveInfo.RecipeInstructionAPIResult>>(json)[0];
            }
        }
    }
    IEnumerator GetRequest(string foodNames, ActiveInfo.RecipeAPIResult result)
    {
        var url = string.Format(RecipesManager.formatUrl, foodNames,ActiveInfo.apiKey);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = url.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                var json = webRequest.downloadHandler.text;
                Debug.Log(pages[page] + ":\nReceived: " + json);
                result.results = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActiveInfo.RecipeAPIItem>>(json);
            }
        }
    }

    IEnumerator GetRequest(string foodName, ActiveInfo.IngredientAPIResult result)
    {
        var url = string.Format("https://api.spoonacular.com/food/ingredients/search?query={0}&number=1&apiKey={1}", foodName,ActiveInfo.apiKey);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = url.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                var json = webRequest.downloadHandler.text;
                Debug.Log(pages[page] + ":\nReceived: " + json);
                result.results = JsonUtility.FromJson<ActiveInfo.IngredientAPIResult>(json).results;
            }
        }
    }

    public void Load(int userID)
    {
        if(userID!=-1 && userID <= int.Parse(DBManager.Select("CAST(COUNT(*) AS CHAR)", "User", "")[0]))
            ActiveInfo.activeUser = ActiveInfo.User.GetUserFromDB(userID);
        else
            ActiveInfo.activeUser = ActiveInfo.User.GetNewUser();
        var fam = ActiveInfo.Family.GetFamilyFromDB(ActiveInfo.activeUser.familyID);
        var storage = fam.storage;
        ActiveInfo.activeUser.familyID = fam.id;
        ActiveInfo.activeStorage = new ActiveInfo.Storage();
        ActiveInfo.activeStorage.items = new List<ActiveInfo.Item>(storage.items);
        ActiveInfo.activeStorage.SortByArchived();
        DBManager.UpdateDB(string.Format("UPDATE User SET JSON = '{0}' WHERE ID = {1};",
                    Newtonsoft.Json.JsonConvert.SerializeObject(ActiveInfo.activeUser),
                    ActiveInfo.activeUser.id));
        print(ActiveInfo.activeUser.familyID);
        StartCoroutine(SyncFromStorage());
    }
}

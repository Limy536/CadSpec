using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddressSearchComponent : MonoBehaviour
{
    #region Public Variables
    [HideInInspector]
    public string fileType = "";
    [HideInInspector]
    public TextAsset file;
    #endregion

    #region Private Variables    
    [Header("UI Buttons")]
    [SerializeField]
    private GameObject _closeButton;
    [SerializeField]
    private GameObject _searchButton;

    private List<string> _occupants = new List<string>();
    private int _numberOfOccupants = 0;
    private GameObject _searchingMessage;
    private GameObject _errorMessage;
    private string _fileLocation = "addresses";
    private int _houseNumber = 0;
    private string _streetName = "";
    #endregion

    void Start()
    {
        Button _close = _closeButton.GetComponent<Button>();
		_close.onClick.AddListener(CloseApplication);

        Button _search = _searchButton.GetComponent<Button>();
		_search.onClick.AddListener(SearchForAddress);

        _searchingMessage = GameObject.Find("SearchingMessage");
        _searchingMessage.SetActive(false);
        _errorMessage = GameObject.Find("ErrorMessage");
        _errorMessage.SetActive(false);

        fileType = "CSV";        
    }

    void Update()
    {
        if (Input.GetKey("escape"))
            CloseApplication();
    
    }

    void CloseApplication()
    {
         Application.Quit();
    }


    bool CheckAddress(string address)
    {
        bool isValid = false;
        int number = 0;
        float time = 0;

        // Check the string isn't empty, it contains a house number and street addess length in the correct format
        if (!String.IsNullOrEmpty(address))
        {
            string[] splitValues = address.Split(' ',2);
            
            if (splitValues.Length > 1)
            {
                if(int.TryParse(splitValues[0],out number))
                {
                    _houseNumber = number;

                    if(splitValues[1].Length >= 4 && !int.TryParse(splitValues[1],out number))
                    {
                        _streetName = splitValues[1];
                        isValid = true;
                    }
                }
            }
        }

        if(isValid)
        {
            // On valid input, the search message appears and the button is disabled until the search is complete
            _errorMessage.SetActive(false);
            _searchingMessage.SetActive(true);
            _searchButton.GetComponent<Button>().enabled = false;
        }
        else
        {
            // notify the user the address is invalid and start couratine to deactivate the message in 8 seconds
            _searchingMessage.SetActive(false);
            _errorMessage.SetActive(true);
            time = 8f;
            StartCoroutine(HideMessage(time));
        }

        return isValid;
    }    

    void SearchForAddress()
    {
        bool addressIsValid = false;
        GameObject inputField = GameObject.Find("AddressInputField");
        GameObject resultText = GameObject.Find("ResultText");
        GameObject numberOfOccupantsText = GameObject.Find("NumberOfOccupantsText");          
        string input = inputField.GetComponent<TMP_InputField>().text;
        
        if(_occupants.Count >= 1)
            _occupants = new List<string>();

        if(_errorMessage.activeSelf)
        {
            //Hide error message if users submits a new address before error message disappears
            StopCoroutine(HideMessage(0f));
            _errorMessage.SetActive(false);
        }

        //Check the address entered is valid
        addressIsValid = CheckAddress(input);
        if(addressIsValid)
        {
            if(fileType != "")
            {
                // Load the first database on occupants at the given address and if the file exists
                // read through the file and use the returned results to update the user 
                
                for(int i = 0; i < 2; i++ )
                {    
                    file = Resources.Load<TextAsset>(fileType + "/" +_fileLocation);

                    if(file != null)
                    {
                        _occupants = ReadFile(file);
                        // Remove dupicates in list using linq and sort in order of age
                        _occupants = _occupants.Distinct().ToList();
                        _occupants = new List<string>(CompareToListAge(_occupants));

                        _numberOfOccupants = _occupants.Count; 
                        numberOfOccupantsText.GetComponent<TMP_Text>().text = "Number of Occupants: " + _numberOfOccupants.ToString();
                        resultText.GetComponent<Text>().text = "";    

                        for(int j = 0; j < _occupants.Count; j++)
                        {
                            _occupants[j] = _occupants[j].Replace("  "," ");
                            resultText.GetComponent<Text>().text += _occupants[j] + "\n";
                        }                                      
                    }
                    else
                        Debug.LogError("File Not Found");

                    // Change the filetype and check the second database for occupants of the given address
                    // We can't assume that all house occupants exist in the same database  
                    if(fileType.Contains("CSV"))
                        fileType = "XML";
                    else if(fileType.Contains("XML"))
                        fileType = "CSV";
                }                      
            }
        }

        if(_searchingMessage.activeSelf)
            _searchingMessage.SetActive(false);

        _searchButton.GetComponent<Button>().enabled = true;            
    }

    List<string> ReadFile(TextAsset text)
    {
        string content = text.text;
        string xmlline = "";
        string[] allWords = content.Split("\n");      
        List<string> people = new List<string>();

        for(int i = 1; i < allWords.Length; i++)
        {
            if(fileType.Contains("CSV"))
            {   
                if(allWords[i] != "")
                {
                    string[] field = allWords[i].Split(",");
                    if(int.Parse(field[3]) == _houseNumber)
                    {
                        if(field[2].Contains(_streetName))
                            people.Add(field[4] + ", " + field[0] + " " + field[1]);
                    }
                }
            }
            else if(fileType.Contains("XML"))
            {
                if(allWords[i].Contains("/Records"))
                    break;                 
                else if(allWords[i] != "" && !allWords[i].Contains("Records") && !allWords[i].Contains("Record"))
                {
                    xmlline += allWords[i] + ",";
                }               
                else if(allWords[i].Contains("/Record"))
                {
                    xmlline = xmlline.Replace('"',' ');
                    string[] field = xmlline.Split(",");
                    string[] cell = field[3].Split("=");

                    if(int.Parse(cell[1]) == _houseNumber)
                    {
                        string[] cell_Age = field[4].Split("=");
                        string[] cell_FirstName = field[0].Split("=");
                        string[] cell_LastName = field[1].Split("=");
                        
                        cell = field[2].Split("=");

                        if(cell[1].Contains(_streetName))
                            people.Add(cell_Age[1] + ", " + cell_FirstName[1] + " " + cell_LastName[1]);
                    }

                    xmlline = null;
                    xmlline = new string(new char[]{});
                }
            }
        }

        if(_searchingMessage.activeSelf)
            _searchingMessage.SetActive(false);

        return people;
    }

     List<string> CompareToListAge(List<string> residents)
    {
        string[] residentsArray = residents.ToArray();
        for(int i = 0; i < residents.Count; i++)
        {
            for(int j = 0; j < residents.Count; j++)
            {
                string[] field_1 = residents[i].Split(",");
                string[] field_2 = residents[j].Split(",");

                if(int.Parse(field_1[0]) < int.Parse(field_2[0]))
                {
                    string tmpLine = residentsArray[i];
                    residentsArray[i] = residentsArray[j];
                    residentsArray[j] = tmpLine;
                }

            }
        }

        residents = new List<string>(residentsArray.ToList());
        return residents;
    }

    IEnumerator HideMessage(float time)
    {
        yield return new WaitForSeconds(time);
        _errorMessage.SetActive(false);
    }
}

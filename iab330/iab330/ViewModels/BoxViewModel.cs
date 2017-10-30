﻿using iab330.Interfaces;
using iab330.Models;
using iab330.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace iab330.ViewModels {
    public class BoxViewModel: BaseViewModel {
        private ObservableCollection<Box> _boxes;
        private BoxDataAccess boxDataAccess;
        private RoomDataAccess roomDataAccess;
        private ItemDataAccess itemDataAccess;
        private string _error;
        private string _newBoxName;
        private Room _selectedRoom = null;

        private Box _boxToBeEdited;
        private string _boxToBeEditedName;
        private string _boxToBeEditedRoomName;
        private ObservableCollection<Item> _boxToBeEditedItems;
        /*
         * At the moment, this is the only way to get a collection of boxes with Room property referencing rooms.
         * When an element is loaded into memory either through database.get method or query, 
         * A property that needs to reference another model is set to null (no recursive retrival).
         * RoomDataAccess's getAllRooms function retrieves all rooms by using GetAllWithChildren which will fill Boxes property
         * Each box in Boxes property will have Room property set. 
         */
        //private ObservableCollection<Box> getBoxesFromRooms(ObservableCollection<Room> rooms) {
        //    ObservableCollection<Box> boxes = new ObservableCollection<Box>();
        //    foreach (Room room in rooms) {
        //        foreach (Box box in room.Boxes) {
        //            boxes.Add(box);
        //        }
        //    }
        //    return boxes;
        //}

        public BoxViewModel() {
            boxDataAccess = DataAccessLocator.BoxDataAccess;
            roomDataAccess = DataAccessLocator.RoomDataAccess;
            itemDataAccess = DataAccessLocator.ItemDataAccess;
            Boxes = boxDataAccess.GetAllBoxes();
            //Boxes = getBoxesFromRooms(ViewModelLocator.RoomsViewModel.Rooms);
            CreateBoxCommand = new Command(
                () => {
                    Error = "";
                    if (String.IsNullOrEmpty(NewBoxName))
                    { //If name field is empty. Not needed?
                        Application.Current.MainPage.DisplayAlert("Error", "Please enter the box's name.", "Ok");
                        //Error = "Please enter the box name";
                        return;
                    }
                    else if (SelectedRoom == null)
                    {//If no room is selected
                        Application.Current.MainPage.DisplayAlert("Error", "No room has been selected. Please select a room.", "Ok");
                        //Error = "Please select a room";
                        return;
                    }
                    else if (false)
                    {
                        //Check to see if the box name is already being used
                    }
                    
                    var newBox = new Box {
                        Name = NewBoxName,
                        //RoomName = SelectedRoom.Name
                    };
                    boxDataAccess.InsertBox(newBox);

                    if (SelectedRoom.Boxes == null) {
                        SelectedRoom.Boxes = new List<Box> { newBox };
                    } else {
                        SelectedRoom.Boxes.Add(newBox);
                    }
                    roomDataAccess.EstablishForeignKey(SelectedRoom);
                    newBox.Items = new List<Item>();
                    Boxes.Add(newBox);

                    NewBoxName = "";
                    SelectedRoom = null;
                },
                () => {
                    return true;
                }
            );

            RemoveBoxCommand = new Command<Box>(
                async (box) =>
                {
                    bool answer = await Application.Current.MainPage.DisplayAlert("Delete Box", "Are you sure you want to delete box?", "Yes", "No");
                    if (answer)
                    {
                        Boxes.Remove(box);
                        boxDataAccess.DeleteBox(box);
                        //May need to improve this
                        ViewModelLocator.ItemViewModel.ItemsToBeEdited = itemDataAccess.GetAllItems();
                    }
                }
            );

            UpdateBoxCommand = new Command(
                () => {
                    if (!string.IsNullOrEmpty(NewBoxName)) {
                        //Should I prevent duplicate box name?
                        BoxToBeEditedName = NewBoxName;
                        BoxToBeEdited.Name = NewBoxName;
                        boxDataAccess.UpdateBox(BoxToBeEdited);
                    }

                    /*
                     * After changing the room, will deleting the previous room delete this box even if it has changed?
                     * test 1: Create a room, then a box, change the box's room then remove the room.
                     * test 2: Create a room then a box. Exit the app. Change the box's room then remove the room.
                     */
                    if (SelectedRoom != null && (BoxToBeEdited.Room != SelectedRoom)) {
                        if (SelectedRoom.Boxes == null) {
                            SelectedRoom.Boxes = new List<Box> { BoxToBeEdited };
                        } else {
                            SelectedRoom.Boxes.Add(BoxToBeEdited);
                        }
                        BoxToBeEditedRoomName = SelectedRoom.Name;
                        BoxToBeEdited.Room.Name = SelectedRoom.Name;
                        roomDataAccess.EstablishForeignKey(SelectedRoom);
                    }

                    

                    SelectedRoom = null;
                    NewBoxName = "";
                    Error = "Edited!";
                    Boxes = boxDataAccess.GetAllBoxes();
                    ViewModelLocator.ItemViewModel.ItemsToBeEdited = itemDataAccess.GetAllItems();
                }
            );

            PrintLabelCommand = new Command(
                () =>
                {
                    var fileService = DependencyService.Get<ICreateLabel>();
                    string itemList = "";
                    var range = BoxToBeEdited.Items.Count;
                    for (int index = 0; index < range; index++)
                    {
                        itemList += "Item Name: " + BoxToBeEdited.Items[index].Name + ", Qty: " + BoxToBeEdited.Items[index].Quantity + Environment.NewLine;
                    }
                    string boxName = "Box Name: " + BoxToBeEdited.Name;
                    string roomName = "Room Type: " + BoxToBeEdited.Room.Name;

                    fileService.SaveFile(boxName, roomName, itemList);
                }
            );
        }

        public ICommand CreateBoxCommand { protected set; get; }
        public ICommand RemoveBoxCommand { protected set; get; }
        public ICommand UpdateBoxCommand { protected set; get; }
        public ICommand PrintLabelCommand { protected set; get; }
        public ObservableCollection<Box> Boxes {
            get { return _boxes; }
            set {
                if (_boxes != value) {
                    _boxes = value;
                    OnPropertyChanged("Boxes");
                }
            }
        }

        public string Error {
            get { return _error; }
            set {
                if (_error != value) {
                    _error = value;
                    OnPropertyChanged("Error");
                }
            }
        }
        

        public string BoxToBeEditedName {
            get { return _boxToBeEditedName; }
            set {
                if (_boxToBeEditedName != value) {
                    _boxToBeEditedName = value;
                    OnPropertyChanged("BoxToBeEditedName");
                }
            }
        }

        public string BoxToBeEditedRoomName {
            get { return _boxToBeEditedRoomName; }
            set {
                if (_boxToBeEditedRoomName != value) {
                    _boxToBeEditedRoomName = value;
                    OnPropertyChanged("BoxToBeEditedRoomName");
                }
            }
        }
        public ObservableCollection<Item> BoxToBeEditedItems {
            get { return _boxToBeEditedItems; }
            set {
                if (_boxToBeEditedItems != value) {
                    _boxToBeEditedItems = value;
                    OnPropertyChanged("BoxToBeEditedItems");
                }
            }
        }

        public string NewBoxName {
            get { return _newBoxName; }
            set {
                if (_newBoxName != value) {
                    _newBoxName = value;
                    OnPropertyChanged("NewBoxName");
                }
            }
        }

        public Room SelectedRoom {
            get {
                return _selectedRoom;
            }
            set {
                if (_selectedRoom != value) {
                    _selectedRoom = value;
                    OnPropertyChanged("SelectedRoom");
                }
            }
        }

        public Box BoxToBeEdited {
            get {
                return _boxToBeEdited;
            }
            set {
                if (_boxToBeEdited != value) {
                    _boxToBeEdited = value;
                    if (_boxToBeEdited != null) {
                        BoxToBeEditedName = _boxToBeEdited.Name;
                        BoxToBeEditedRoomName = _boxToBeEdited.Room.Name;
                        BoxToBeEditedItems = new ObservableCollection<Item>(_boxToBeEdited.Items);
                    }
                    OnPropertyChanged("BoxToBeEdited");
                }
            }
        }
    }
}

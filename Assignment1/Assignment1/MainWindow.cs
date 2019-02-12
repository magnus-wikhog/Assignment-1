using System;
using System.Collections.Generic;
using Assignment1;
using Assignment1.Animals;
using Gtk;


/* 
 * Class that represents a node in the animals list
 */ 
[Gtk.TreeNode(ListOnly = true)]
public class AnimalNode : Gtk.TreeNode {

    public AnimalNode(string id, Animal animal) {
        this.id = id;
        this.type = animal.GetType().Name;
        this.name = animal.name;
        this.age = animal.age.ToString();
        this.gender = animal.gender;
        this.specialCharacteristics = animal.GetSpecialCharacteristics();
    }

    [Gtk.TreeNodeValue(Column = 0)]
    public string id;

    [Gtk.TreeNodeValue(Column = 1)]
    public string type;

    [Gtk.TreeNodeValue(Column = 2)]
    public string name;

    [Gtk.TreeNodeValue(Column = 3)]
    public string age;

    [Gtk.TreeNodeValue(Column = 4)]
    public string gender;

    [Gtk.TreeNodeValue(Column = 5)]
    public string specialCharacteristics;
}


/* 
 * Class that represents a species in the species list
 */
[Gtk.TreeNode(ListOnly = true)]
public class SpeciesNode : Gtk.TreeNode {

    public SpeciesNode(string name) {
        this.name = name;
    }

    [Gtk.TreeNodeValue(Column = 0)]
    public string name;
}


/* 
 * Class that represents a category in the category list
 */
[Gtk.TreeNode(ListOnly = true)]
public class CategoryNode : Gtk.TreeNode {

    public CategoryNode(string name) {
        this.name = name;
    }

    [Gtk.TreeNodeValue(Column = 0)]
    public string name;
}


/* 
 * Class that represents a configuration for a category (for example which 
 * input page to display).
 */
class CategoryConfiguration {
    public string name;
    public int inputPage;

    public CategoryConfiguration(string name, int inputPage) {
        this.name = name;
        this.inputPage = inputPage;
    }
}


/* 
 * Class that represents a configuration for a species (for example which 
 * input page to display).
 */
class SpeciesConfiguration {
    public string name;
    public int inputPage;
    public CategoryConfiguration categoryConfiguration;

    public SpeciesConfiguration(string name, int inputPage, CategoryConfiguration categoryConfiguration) {
        this.name = name;
        this.inputPage = inputPage;

        // Determines which category configuration to use for this species
        this.categoryConfiguration = categoryConfiguration; 
    }
}




public partial class MainWindow : Gtk.Window {

    private AnimalManager mAnimalManager;
    private Dictionary<string, CategoryConfiguration> mCategoriesConfigurations;
    private Dictionary<string, SpeciesConfiguration> mSpeciesConfigurations;


    public MainWindow() : base(Gtk.WindowType.Toplevel) {
        Build();

        // Our animal manager
        mAnimalManager = new AnimalManager();

        // Build animal list view
        animalsNodeView.NodeStore = new NodeStore(typeof(AnimalNode));
        animalsNodeView.AppendColumn("ID", new Gtk.CellRendererText(), "text", 0);
        animalsNodeView.AppendColumn("Species", new Gtk.CellRendererText(), "text", 1);
        animalsNodeView.AppendColumn("Name", new Gtk.CellRendererText(), "text", 2);
        animalsNodeView.AppendColumn("Age", new Gtk.CellRendererText(), "text", 3);
        animalsNodeView.AppendColumn("Gender", new Gtk.CellRendererText(), "text", 4);
        animalsNodeView.AppendColumn("Special characteristics", new Gtk.CellRendererText(), "text", 5);
        animalsNodeView.ShowAll();

        // Build species list view
        speciesNodeView.NodeStore = new NodeStore(typeof(SpeciesNode));
        speciesNodeView.AppendColumn("Species", new CellRendererText(), "text", 0);
        speciesNodeView.NodeSelection.Changed += new System.EventHandler(OnSpeciesSelectionChanged);
        speciesNodeView.ShowAll();

        // Build category list view
        categoryNodeView.NodeStore = new NodeStore(typeof(CategoryNode));
        categoryNodeView.AppendColumn("Category", new CellRendererText(), "text", 0);
        categoryNodeView.NodeSelection.Changed += new System.EventHandler(OnCategorySelectionChanged);
        categoryNodeView.ShowAll();

        // Configrure categories
        mCategoriesConfigurations = new Dictionary<string, CategoryConfiguration> {
            { "Mammal", new CategoryConfiguration("Mammal", 1) },
            { "Bird",   new CategoryConfiguration("Bird",   0) }
        };

        // COnfigure species
        mSpeciesConfigurations = new Dictionary<string, SpeciesConfiguration> {
            { "Cat",   new SpeciesConfiguration("Cat",  0, mCategoriesConfigurations["Mammal"] ) },
            { "Dog",   new SpeciesConfiguration("Dog",  1, mCategoriesConfigurations["Mammal"] ) },
            { "Swan",  new SpeciesConfiguration("Swan", 2, mCategoriesConfigurations["Bird"]   ) },
            { "Crow",  new SpeciesConfiguration("Crow", 3, mCategoriesConfigurations["Bird"]   ) }
        };

        // Add configured categories to the list view
        foreach (string category in mCategoriesConfigurations.Keys) {
            categoryNodeView.NodeStore.AddNode(new CategoryNode(category));
        }

    }



    protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
        Application.Quit();
        a.RetVal = true;
    }



    /*
     * Updates the UI in response to a selected category
     */    
    void OnCategorySelected(CategoryNode categoryNode) {
        speciesNodeView.NodeStore.Clear();
        foreach (SpeciesConfiguration species in mSpeciesConfigurations.Values) {
            if ( null == categoryNode || species.categoryConfiguration.name.Equals(categoryNode.name) )
                speciesNodeView.NodeStore.AddNode(new SpeciesNode(species.name));
        }
        speciesNodeView.NodeSelection.SelectNode(speciesNodeView.NodeStore.GetNode(new TreePath(new int[]{ 0 })));
    }

    /*
     * Event handler for the category list
     */
    void OnCategorySelectionChanged(object o, System.EventArgs args) {
        OnCategorySelected( (CategoryNode)((NodeSelection)o).SelectedNode );
    }

    /*
     * Updates the UI when the user checks/unchecks "Show all species"
     */
    protected void OnShowAllSpeciesCheckboxToggled(object sender, EventArgs e) {
        categoryNodeView.Sensitive = !showAllSpeciesCheckbox.Active;
        OnCategorySelected(showAllSpeciesCheckbox.Active ? null : (CategoryNode)categoryNodeView.NodeSelection.SelectedNode);
    }

    /*
     * Updates the UI when the user selects a species
     */
    void OnSpeciesSelectionChanged(object o, System.EventArgs args) {
        SpeciesNode speciesNode = (SpeciesNode)((NodeSelection)o).SelectedNode;

        // speciesNode could be null when the list is cleared!
        if (null != speciesNode) {
            // Activate correct pages for the selected species
            // Category page must also be actived from here, because we could be showing animals from
            // all categories in the listview.
            SpeciesConfiguration speciesConfiguration = mSpeciesConfigurations[speciesNode.name];
            speciesInputPager.CurrentPage = speciesConfiguration.inputPage;
            categoryInputPager.CurrentPage = speciesConfiguration.categoryConfiguration.inputPage;
        }
    }



    /*
     * Adds an animal with the current specification to the list.
     */
    protected void OnAddAnimalButtonClicked(object sender, EventArgs e) {
        SpeciesNode speciesNode = (SpeciesNode)((NodeSelection) speciesNodeView.NodeSelection).SelectedNode;

        // Determine which type of animal to create, and use the correct UI elements to set it's properties
        Animal animal;
        switch (speciesNode.name) {
            case "Cat":  animal = new Cat((int)mammalTeethCountSpinner.Value, catClawlengthSpinner.Value); break;
            case "Dog":  animal = new Dog((int)mammalTeethCountSpinner.Value, dogTailLengthSpinner.Value); break;
            case "Swan": animal = new Swan(birdWingspanSpinner.Value, swanColorEntry.Text); break;
            case "Crow": animal = new Crow(birdWingspanSpinner.Value, crowWeightSpinner.Value); break;
            default: return; // Don't add anything if it's an unknown species name
        }

        // Set common animal properties
        animal.name = nameEntry.Text;
        if (!int.TryParse(ageSpinButton.Text, out animal.age)) animal.age = 0;
        animal.gender = genderCombo.ActiveText;

        // Add the animal to the list
        animalsNodeView.NodeStore.AddNode(mAnimalManager.AddAnimal(animal));
    }


}


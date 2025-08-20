
using System.Text.Json.Serialization;

namespace Hv2UI;

public class PageContainer
{
	[JsonInclude] public string PageID;
	[JsonInclude] public string PageName;

	[JsonInclude] internal List<DataEntryField> Fields = [];

	internal PagedScrollableDataEntry AttachedInstance;

	internal int ScrollY = 0; // Index that tracks where in the menu we have scrolled to
	internal int ScrollYMax => VisibleFieldCount <= AttachedInstance.Height ? 0 : VisibleFieldCount - AttachedInstance.Height;

	internal DataEntryField[] VisibleFields => Fields.Where(IsFieldVisible).ToArray();
	internal int VisibleFieldCount => Fields.Count(IsFieldVisible);

	[JsonIgnore] public DataEntryField CurrentField => Fields[SelectedFieldIndex];
	public int SelectedFieldIndex { get; internal set; }

	private bool IsFieldVisible(DataEntryField Field)
	{
		if (Field.VisibilityRule is null)
			return true;

		try
		{
			return Field.VisibilityRule();
		}
		catch (Exception)
		{
			return true;
		}
	}

	public DataEntryField this[string ID]
	{
		get
		{
			return Fields.FirstOrDefault(f => f.ID == ID);
		}

		set
		{
			var test = this[ID];

			if (test is null)
			{
				value.ID = ID;
				AddField(value);
			}
			else
			{
				Fields[Fields.IndexOf(test)] = value;
			}
		}
	}

	internal void Previous()
	{
		if (SelectedFieldIndex == 0)
		{
			SelectedFieldIndex = VisibleFieldCount - 1;
			ScrollY = ScrollYMax;
		}
		else
		{
			SelectedFieldIndex--;

			if (SelectedFieldIndex < ScrollY)
				ScrollY = SelectedFieldIndex;
		}

		if (AttachedInstance.OnSelectionChange is not null)
			AttachedInstance.OnSelectionChange(AttachedInstance.CurrentlySelectedField.ID, AttachedInstance.CurrentlySelectedField);

		// Field-specific actions

		switch (AttachedInstance.CurrentlySelectedField)
		{
			case TextField tf:
				tf.defInputField.EnsureCursorVisible();
				break;
		}
	}

	internal void Next()
	{
		if (SelectedFieldIndex == VisibleFieldCount - 1)
		{
			SelectedFieldIndex = 0;
			ScrollY = 0;
		}
		else
		{
			SelectedFieldIndex++;

			if (SelectedFieldIndex - ScrollY > AttachedInstance.Height - 1)
				ScrollY++;
		}

		if (AttachedInstance.OnSelectionChange is not null)
			AttachedInstance.OnSelectionChange(AttachedInstance.CurrentlySelectedField.ID, AttachedInstance.CurrentlySelectedField);

		// Field-specific actions

		switch (AttachedInstance.CurrentlySelectedField)
		{
			case TextField tf:
				tf.defInputField.EnsureCursorVisible();
				break;
		}
	}

	public void AddField(DataEntryField Field)
	{
		if (Fields.Any(f => f.ID == Field.ID))
			return;

		Fields.Add(Field);
	}
}

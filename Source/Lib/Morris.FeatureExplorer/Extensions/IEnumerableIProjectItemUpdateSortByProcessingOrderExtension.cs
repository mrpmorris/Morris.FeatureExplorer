#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class IEnumerableIProjectItemUpdateSortByProcessingOrderExtension
{
	public static IEnumerable<IProjectItemUpdate<IEntityWithId>> SortByProcessingOrder(this IEnumerable<IProjectItemUpdate<IEntityWithId>> source) =>
		source
		.OrderBy(x =>
			x switch
			{
				// 1: Modified projects
				IProjectItemUpdate<IProjectSnapshot> modifiedProject
					when modifiedProject.UpdateType is UpdateType.Updated => 1,

				// 2: Modified folders
				IProjectItemUpdate<IFolderSnapshot> modifiedFolder
					when modifiedFolder.UpdateType is UpdateType.Updated => 2,

				// 3: Modified files
				IProjectItemUpdate<IFileSnapshot> modifiedFile
					when modifiedFile.UpdateType is UpdateType.Updated => 3,

				// 4: Added projects
				IProjectItemUpdate<IProjectSnapshot> addedProject
					when addedProject.UpdateType is UpdateType.Added => 4,

				// 5: Added folders
				IProjectItemUpdate<IFolderSnapshot> addedFolder
					when addedFolder.UpdateType is UpdateType.Added => 5,

				// 6: Added files
				IProjectItemUpdate<IFileSnapshot> addedFile
					when addedFile.UpdateType is UpdateType.Added => 6,

				// 7: Removed files
				IProjectItemUpdate<IFileSnapshot> removedFile
					when removedFile.UpdateType is UpdateType.Removed => 7,

				// 8: Removed folders
				IProjectItemUpdate<IFolderSnapshot> removedFolder
					when removedFolder.UpdateType is UpdateType.Removed => 8,

				// 9: Removed projects
				IProjectItemUpdate<IProjectSnapshot> removedProject
					when removedProject.UpdateType is UpdateType.Removed => 9,

				_ => throw new NotImplementedException(x.GetType().Name)
			}
		)
		.ThenBy(x =>
			x switch
			{
				// 1st Add folders = shortest first (add parents before children)
				IProjectItemUpdate<IFolderSnapshot> folder
					when folder.UpdateType is UpdateType.Added
					=> x.Current!.Id!.GetFolderPath().Length,

				// 2nd Removed folders = longest first (remove children before parents)
				IProjectItemUpdate<IFolderSnapshot> folder
					when folder.UpdateType is UpdateType.Removed
					=> 0 - x.PreviousId!.GetFolderPath().Length,

				// Add/Update/Remove either Projects/Files,
				// or Update folders = no order preference
				_ => 0
			}
		);
}

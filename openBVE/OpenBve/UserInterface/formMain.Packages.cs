﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenBveApi.Packages;

namespace OpenBve
{
	internal partial class formMain
	{
		/*
		 * This class contains the drawing and management routines for the
		 * package management tab of the main form.
		 * 
		 * Package manipulation is handled by the OpenBveApi.Packages namespace
		 */
		
		internal int selectedTrainPackageIndex = 0;
		internal int selectedRoutePackageIndex = 0;
		internal static bool creatingPackage = false;
		internal PackageType newPackageType;
		internal string ImageFile;
		internal BackgroundWorker workerThread = new BackgroundWorker();


		internal void RefreshPackages()
		{
			SavePackages();
			LoadPackages();
			PopulatePackageList(currentDatabase.InstalledRoutes, dataGridViewRoutePackages, true);
			PopulatePackageList(currentDatabase.InstalledTrains, dataGridViewTrainPackages, true);
			PopulatePackageList(currentDatabase.InstalledOther, dataGridViewInstalledOther, true);
		}

		private Package currentPackage;
		private Package oldPackage;
		private List<PackageFile> filesToPackage;

		private void buttonInstall_Click(object sender, EventArgs e)
		{
			if (creatingPackage)
			{
				if (textBoxPackageName.Text == Interface.GetInterfaceString("packages_selection_none"))
				{
					MessageBox.Show(Interface.GetInterfaceString("packages_creation_name"));
					return;
				}
				if (textBoxPackageAuthor.Text == Interface.GetInterfaceString("packages_selection_none"))
				{
					MessageBox.Show(Interface.GetInterfaceString("packages_creation_author"));
					return;
				}
				//LINK: Doesn't need checking
				if (textBoxPackageDescription.Text == Interface.GetInterfaceString("packages_selection_none"))
				{
					MessageBox.Show(Interface.GetInterfaceString("packages_creation_description"));
					return;
				}
				if (!Version.TryParse(textBoxPackageVersion.Text, out currentPackage.PackageVersion))
				{
					MessageBox.Show(Interface.GetInterfaceString("packages_creation_version"));
				}
				//Only set properties after making the checks
				currentPackage.Name = textBoxPackageName.Text;
				currentPackage.Author = textBoxPackageAuthor.Text;
				currentPackage.Description = textBoxPackageDescription.Text.Replace("\r\n","\\r\\n");

				HidePanels();
				panelPackageDependsAdd.Show();
				PopulatePackageList(currentDatabase.InstalledRoutes, dataGridViewRoutes, false);
				PopulatePackageList(currentDatabase.InstalledTrains, dataGridViewTrains, false);
				return;
			}
			//Check to see if the package is null- If null, then we haven't loaded a package yet
			if (currentPackage == null)
			{
				if (openPackageFileDialog.ShowDialog() == DialogResult.OK)
				{
					currentPackage = Manipulation.ReadPackage(openPackageFileDialog.FileName);
					if (currentPackage != null)
					{
						textBoxPackageName.Text = currentPackage.Name;
						textBoxPackageAuthor.Text = currentPackage.Author;
						if (currentPackage.Description != null)
						{
							textBoxPackageDescription.Text = currentPackage.Description.Replace("\\r\\n", "\r\n");
						}
						textBoxPackageVersion.Text = currentPackage.PackageVersion.ToString();
						if (currentPackage.Website != null)
						{
							linkLabelPackageWebsite.Text = currentPackage.Website;
							LinkLabel.Link link = new LinkLabel.Link {LinkData = currentPackage.Website};
							linkLabelPackageWebsite.Links.Add(link);
						}
						else
						{
							linkLabelPackageWebsite.Text = Interface.GetInterfaceString("packages_selection_none_website");
						}
						if (currentPackage.PackageImage != null)
						{
							pictureBoxPackageImage.Image = currentPackage.PackageImage;
						}
						else
						{
							TryLoadImage(pictureBoxPackageImage, currentPackage.PackageType == 0 ? "route_unknown.png" : "train_unknown.png");
						}
						buttonSelectPackage.Text = Interface.GetInterfaceString("packages_install");
					}
					else
					{
						//ReadPackage returns null if the file is not a package.....
						MessageBox.Show(Interface.GetInterfaceString("packages_install_invalid"));
					}
				}

			}
			else
			{
				List<Package> Dependancies = Information.CheckDependancies(currentPackage, currentDatabase.InstalledRoutes, currentDatabase.InstalledTrains);
				if (Dependancies != null)
				{
					//We are missing a dependancy
					PopulatePackageList(Dependancies, dataGridViewDependancies, false);
					HidePanels();
					panelDependancyError.Show();
					return;
				}
				VersionInformation Info;
				oldPackage = null;
				switch (currentPackage.PackageType)
				{
					case PackageType.Route:
						Info = Information.CheckVersion(currentPackage, currentDatabase.InstalledRoutes, ref oldPackage);
						break;
					case PackageType.Train:
						Info = Information.CheckVersion(currentPackage, currentDatabase.InstalledTrains, ref oldPackage);
						break;
					default:
						Info = Information.CheckVersion(currentPackage, currentDatabase.InstalledOther, ref oldPackage);
						break;
				}
				if (Info == VersionInformation.NotFound)
				{
					panelPackageInstall.Hide();
					Extract();
				}
				else
				{
					switch (Info)
					{
						case VersionInformation.NewerVersion:
							labelVersionError.Text = Interface.GetInterfaceString("packages_install_version_new");
							textBoxCurrentVersion.Text = oldPackage.PackageVersion.ToString();
							break;
						case VersionInformation.SameVersion:
							labelVersionError.Text = Interface.GetInterfaceString("packages_install_version_same");
							textBoxCurrentVersion.Text = currentPackage.PackageVersion.ToString();
							break;
						case VersionInformation.OlderVersion:
							labelVersionError.Text = Interface.GetInterfaceString("packages_install_version_old");
							textBoxCurrentVersion.Text = oldPackage.PackageVersion.ToString();
							break;
					}
					textBoxNewVersion.Text = currentPackage.PackageVersion.ToString();
					if (currentPackage.Dependancies.Count != 0)
					{
						List<Package> brokenDependancies = OpenBveApi.Packages.Information.UpgradeDowngradeDependancies(currentPackage, currentDatabase.InstalledRoutes, currentDatabase.InstalledTrains);
						if (brokenDependancies != null)
						{
							PopulatePackageList(brokenDependancies, dataGridViewBrokenDependancies, false);
						}
					}
					HidePanels();
					panelVersionError.Show();
				}
			}
				
		}

		private void buttonInstallFinished_Click(object sender, EventArgs e)
		{
			RefreshPackages();
			ResetInstallerPanels();
		}

		private static PackageDatabase currentDatabase = new PackageDatabase
		{
			InstalledRoutes =  new List<Package>(),
			InstalledTrains = new List<Package>(),
			InstalledOther =  new List<Package>()
		};
		private static readonly string currentDatabaseFolder = OpenBveApi.Path.CombineDirectory(Program.FileSystem.SettingsFolder, "PackageDatabase");
		private static readonly string currentDatabaseFile = OpenBveApi.Path.CombineFile(currentDatabaseFolder, "packages.xml");

		private void buttonProceedAnyway_Click(object sender, EventArgs e)
		{
			Extract(oldPackage);
		}

		private void Extract(Package packageToReplace = null)
		{
			panelPleaseWait.Show();
			workerThread.DoWork += delegate
			{
				string ExtractionDirectory;
				switch (currentPackage.PackageType)
				{
					case PackageType.Route:
						ExtractionDirectory = Program.FileSystem.InitialRailwayFolder;
						break;
					case PackageType.Train:
						ExtractionDirectory = Program.FileSystem.InitialTrainFolder;
						break;
					default:
						//TODO: Not sure this is the right place to put this, but at the moment leave it there
						ExtractionDirectory = Program.FileSystem.DataFolder;
						break;
				}
				string PackageFiles = "";
				Manipulation.ExtractPackage(currentPackage, ExtractionDirectory, currentDatabaseFolder, ref PackageFiles);
				textBoxFilesInstalled.Invoke((MethodInvoker) delegate
				{
					textBoxFilesInstalled.Text = PackageFiles;
				});
			};
			workerThread.RunWorkerCompleted += delegate
			{
				switch (currentPackage.PackageType)
				{
					case PackageType.Route:
						if (packageToReplace != null)
						{
							for (int i = currentDatabase.InstalledRoutes.Count; i > 0; i--)
							{
								if (currentDatabase.InstalledRoutes[i - 1].GUID == currentPackage.GUID)
								{
									currentDatabase.InstalledRoutes.Remove(currentDatabase.InstalledRoutes[i - 1]);
								}
							}
						}
						currentDatabase.InstalledRoutes.Add(currentPackage);
						break;
					case PackageType.Train:
						if (packageToReplace != null)
						{
							for (int i = currentDatabase.InstalledTrains.Count; i > 0; i--)
							{
								if (currentDatabase.InstalledTrains[i - 1].GUID == currentPackage.GUID)
								{
									currentDatabase.InstalledTrains.Remove(currentDatabase.InstalledTrains[i - 1]);
								}
							}
						}
						currentDatabase.InstalledTrains.Add(currentPackage);
						break;
				}
				labelInstallSuccess1.Text = Interface.GetInterfaceString("packages_install_success");
				labelInstallSuccess2.Text = Interface.GetInterfaceString("packages_install_header");
				labelListFilesInstalled.Text = Interface.GetInterfaceString("packages_install_files");
				panelPleaseWait.Hide();
				panelSuccess.Show();
			};
			workerThread.RunWorkerAsync();
		}

		/// <summary>Call this method to save the package list to disk.</summary>
		internal void SavePackages()
		{
			try
			{
				if (!Directory.Exists(currentDatabaseFolder))
				{
					Directory.CreateDirectory(currentDatabaseFolder);
				}
				if (File.Exists(currentDatabaseFile))
				{
					File.Delete(currentDatabaseFile);
				}
				using (StreamWriter sw = new StreamWriter(currentDatabaseFile))
				{
						XmlSerializer listWriter = new XmlSerializer(typeof(PackageDatabase));
						listWriter.Serialize(sw, currentDatabase);
				}
			}
			catch (Exception)
			{
				MessageBox.Show(Interface.GetInterfaceString("packages_database_save_error"));
			}
		}

		internal bool LoadPackages()
		{
			try
			{
				XmlSerializer listReader = new XmlSerializer(typeof(PackageDatabase));
				currentDatabase = (PackageDatabase) listReader.Deserialize(XmlReader.Create(currentDatabaseFile));
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// <summary>This method should be called to populate a datagrid view with a list of packages</summary>
		/// <param name="packageList">The list of packages</param>
		/// <param name="dataGrid">The datagrid view to populate</param>
		/// <param name="simpleList">Whether this is a simple list or a dependancy list</param>
		internal void PopulatePackageList(List<Package> packageList, DataGridView dataGrid, bool simpleList)
		{
			//Clear the package list
			dataGrid.Rows.Clear();
			//We have route packages in our list!
			for (int i = 0; i < packageList.Count; i++)
			{
				//Create row
				object[] packageToAdd;
				if (!simpleList)
				{
					packageToAdd = new object[]
					{
						packageList[i].Name, packageList[i].MinimumVersion, packageList[i].MaximumVersion, packageList[i].Author,
						packageList[i].Website
					};
				}
				else
				{
					packageToAdd = new object[]
					{
						packageList[i].Name, packageList[i].Version, packageList[i].Author, packageList[i].Website
					};
				}
				//Add to the datagrid view
				dataGrid.Rows.Add(packageToAdd);
			}
		}

		/// <summary>This method should be called to uninstall a package</summary>
		internal void UninstallPackage(int selectedPackageIndex, ref List<Package> Packages)
		{
			
			string uninstallResults = "";
			Package packageToUninstall = Packages[selectedPackageIndex];
			List<Package> brokenDependancies = DatabaseFunctions.CheckUninstallDependancies(currentDatabase, packageToUninstall.Dependancies);
			if (brokenDependancies.Count != 0)
			{
				PopulatePackageList(brokenDependancies, dataGridViewBrokenDependancies, false);
				labelMissingDepemdanciesText1.Text = Interface.GetInterfaceString("packages_uninstall_broken");
				panelDependancyError.Show();
			}
			if (Manipulation.UninstallPackage(packageToUninstall, currentDatabaseFolder ,ref uninstallResults))
			{
				Packages.Remove(packageToUninstall);
				textBoxUninstallResult.Text = uninstallResults;
				HidePanels();
				panelUninstallResult.Show();
			}
			else
			{
				if (uninstallResults == null)
				{
					//Uninstall requires an XML list of files, and these were missing.......
					textBoxUninstallResult.Text = Interface.GetInterfaceString("packages_uninstall_missing_xml");
				}
			}
			panelUninstallResult.Show();
		}



		internal void AddDependendsReccomends(Package packageToAdd, ref List<Package> DependsReccomendsList)
		{
			if (currentPackage != null)
			{
				if (DependsReccomendsList == null)
				{
					DependsReccomendsList = new List<Package>();
				}
				DependsReccomendsList.Add(packageToAdd);
			}
		}



		private void dataGridViewTrainPackages_SelectionChanged(object sender, EventArgs e)
		{
			selectedRoutePackageIndex = -1;
			selectedTrainPackageIndex = -1;
			if (dataGridViewTrainPackages.SelectedRows.Count > 0)
			{
				selectedTrainPackageIndex = dataGridViewTrainPackages.SelectedRows[0].Index;
			}
		}

		private void dataGridViewRoutePackages_SelectionChanged(object sender, EventArgs e)
		{
			selectedTrainPackageIndex = -1;
			selectedRoutePackageIndex = -1;
			if (dataGridViewRoutePackages.SelectedRows.Count > 0)
			{
				selectedRoutePackageIndex = dataGridViewRoutePackages.SelectedRows[0].Index;
			}
		}

		private void buttonUninstallPackage_Click(object sender, EventArgs e)
		{
			if (selectedRoutePackageIndex != -1)
			{
				UninstallPackage(selectedRoutePackageIndex, ref currentDatabase.InstalledRoutes);
			}
			if (selectedTrainPackageIndex != -1)
			{
				UninstallPackage(selectedTrainPackageIndex, ref currentDatabase.InstalledTrains);
			}
		}

		private void buttonUninstallFinish_Click(object sender, EventArgs e)
		{
			HidePanels();
			panelPackageList.Show();
		}


		private void buttonInstallPackage_Click(object sender, EventArgs e)
		{
			labelInstallText.Text = Interface.GetInterfaceString("packages_install_header");
			TryLoadImage(pictureBoxPackageImage, "route_error.png");
			HidePanels();
			panelPackageInstall.Show();
		}

		private void buttonDepends_Click(object sender, EventArgs e)
		{
			if (selectedTrainPackageIndex != -1)
			{
				Package tempPackage = currentDatabase.InstalledTrains[selectedTrainPackageIndex];
				tempPackage.Version = null;
				if (ShowVersionDialog(ref tempPackage.MinimumVersion, ref tempPackage.MaximumVersion, tempPackage.Version) ==
				    DialogResult.OK)
				{
					AddDependendsReccomends(tempPackage, ref currentPackage.Dependancies);
				}			
			}
			if (selectedRoutePackageIndex != -1)
			{
				Package tempPackage = currentDatabase.InstalledRoutes[selectedRoutePackageIndex];
				tempPackage.Version = null;
				if (ShowVersionDialog(ref tempPackage.MinimumVersion, ref tempPackage.MaximumVersion, tempPackage.Version) ==
					DialogResult.OK)
				{
					AddDependendsReccomends(tempPackage, ref currentPackage.Dependancies);
				}
			}
		}

		private void buttonReccomends_Click(object sender, EventArgs e)
		{
			if (selectedTrainPackageIndex != -1)
			{
				Package tempPackage = currentDatabase.InstalledTrains[selectedTrainPackageIndex];
				tempPackage.Version = null;
				if (ShowVersionDialog(ref tempPackage.MinimumVersion, ref tempPackage.MaximumVersion, tempPackage.Version) ==
					DialogResult.OK)
				{
					AddDependendsReccomends(tempPackage, ref currentPackage.Dependancies);
				}
			}
			if (selectedRoutePackageIndex != -1)
			{
				Package tempPackage = currentDatabase.InstalledRoutes[selectedRoutePackageIndex];
				tempPackage.Version = null;
				if (ShowVersionDialog(ref tempPackage.MinimumVersion, ref tempPackage.MaximumVersion, tempPackage.Version) ==
					DialogResult.OK)
				{
					AddDependendsReccomends(tempPackage, ref currentPackage.Dependancies);
				}
			}
		}

		private void dataGridViewRoutes_SelectionChanged(object sender, EventArgs e)
		{
			selectedTrainPackageIndex = -1;
			selectedRoutePackageIndex = -1;
			if (dataGridViewRoutes.SelectedRows.Count > 0)
			{
				selectedRoutePackageIndex = dataGridViewRoutes.SelectedRows[0].Index;
			}
		}

		private void dataGridViewTrains_SelectionChanged(object sender, EventArgs e)
		{
			selectedRoutePackageIndex = -1;
			selectedTrainPackageIndex = -1;
			if (dataGridViewTrains.SelectedRows.Count > 0)
			{
				selectedTrainPackageIndex = dataGridViewTrains.SelectedRows[0].Index;
			}
		}

		private void buttonCreatePackage_Click(object sender, EventArgs e)
		{
			HidePanels();
			panelPleaseWait.Show();
			workerThread.DoWork += delegate
			{
				Manipulation.CreatePackage(currentPackage, currentPackage.FileName, ImageFile, filesToPackage);
				string text = "";
				for (int i = 0; i < filesToPackage.Count; i++)
				{
					text += filesToPackage[i].relativePath + "\r\n";
				}
				textBoxFilesInstalled.Invoke((MethodInvoker) delegate
				{
					textBoxFilesInstalled.Text = text;
				});
				System.Threading.Thread.Sleep(10000);
			};

			workerThread.RunWorkerCompleted += delegate {
				labelInstallSuccess1.Text = Interface.GetInterfaceString("packages_creation_success");
				labelInstallSuccess2.Text = Interface.GetInterfaceString("packages_creation_success_header");
				labelListFilesInstalled.Text = Interface.GetInterfaceString("packages_creation_success_files");
				label1.Text = "Finished!";
				panelPleaseWait.Hide();
				panelSuccess.Show();
			};

			 workerThread.RunWorkerAsync();	
		}

		private void button3_Click(object sender, EventArgs e)
		{
			System.IO.FileInfo fi = null;
			try
			{
				fi = new System.IO.FileInfo(currentPackage.FileName);
			}
			catch
			{
			}
			if (fi == null)
			{
				MessageBox.Show(Interface.GetInterfaceString("packages_creation_invalid_filename"));
				return;
			}
			buttonSelectPackage.Text = Interface.GetInterfaceString("packages_creation_proceed");
			creatingPackage = true;
			switch (newPackageType)
			{
				case PackageType.Route:
					TryLoadImage(pictureBoxPackageImage, "route_unknown.png");
					break;
				case PackageType.Train:
					TryLoadImage(pictureBoxPackageImage, "train_unknown.png");
					break;
				default:
					TryLoadImage(pictureBoxPackageImage, "logo.png");
					break;
			}
			labelInstallText.Text = Interface.GetInterfaceString("packages_creation_header");
			textBoxPackageName.Text = currentPackage.Name;
			textBoxPackageVersion.Text = currentPackage.Version;
			textBoxPackageAuthor.Text = currentPackage.Author;
			if (currentPackage.Description != null)
			{
				textBoxPackageDescription.Text = currentPackage.Description.Replace("\\r\\n", "\r\n");
			}
			HidePanels();
			panelPackageInstall.Show();
		}

		private void createPackageButton_Click(object sender, EventArgs e)
		{
			HidePanels();
			panelCreatePackage.Show();
		}

		private void pictureBoxPackageImage_Click(object sender, EventArgs e)
		{
			if (creatingPackage)
			{
				if (openPackageFileDialog.ShowDialog() == DialogResult.OK)
				{
					ImageFile = openPackageFileDialog.FileName;
					pictureBoxPackageImage.Image = Image.FromFile(openPackageFileDialog.FileName);
				}
			}
		}


		private void Q1_CheckedChanged(object sender, EventArgs e)
		{
			filesToPackage = null;
			filesToPackageBox.Lines = null;
			if (radioButtonQ1Yes.Checked == true)
			{
				panelReplacePackage.Show();
				panelNewPackage.Hide();
				switch (newPackageType)
				{
					case PackageType.Route:
						PopulatePackageList(currentDatabase.InstalledRoutes, dataGridViewReplacePackage, false);
						break;
					case PackageType.Train:
						PopulatePackageList(currentDatabase.InstalledTrains, dataGridViewReplacePackage, false);
						break;
					case PackageType.Other:
						PopulatePackageList(currentDatabase.InstalledOther, dataGridViewReplacePackage, false);
						break;
				}
				dataGridViewReplacePackage.ClearSelection();
			}
			else
			{
				panelReplacePackage.Hide();
				panelNewPackage.Show();
				panelNewPackage.Enabled = true;
				string GUID = Guid.NewGuid().ToString();
				currentPackage = new Package
				{
					Name = textBoxPackageName.Text,
					Author = textBoxPackageAuthor.Text,
					Description = textBoxPackageDescription.Text.Replace("\r\n", "\\r\\n"),
					//TODO:
					//Website = linkLabelPackageWebsite.Links[0],
					GUID = GUID,
					PackageVersion = new Version(0, 0, 0, 0),
					PackageType = newPackageType
				};
				textBoxGUID.Text = currentPackage.GUID;
				SaveFileNameButton.Enabled = true;
			}
		}

		private void Q2_CheckedChanged(object sender, EventArgs e)
		{
			filesToPackage = null;
			filesToPackageBox.Lines = null;
			radioButtonQ1Yes.Enabled = true;
			radioButtonQ1No.Enabled = true;
			labelReplacePackage.Enabled = true;
			if (radioButtonQ2Route.Checked)
			{
				newPackageType = PackageType.Route;
			}
			else if (radioButtonQ2Train.Checked)
			{
				newPackageType = PackageType.Train;
			}
			else
			{
				newPackageType = PackageType.Other;
			}
			panelReplacePackage.Hide();
			panelNewPackage.Show();
			panelNewPackage.Enabled = false;
			if (radioButtonQ1Yes.Checked || radioButtonQ1No.Checked)
			{
				//Don't generate a new GUID if we haven't decided whether this is a new/ replacement package
				//Pointless & confusing UI update otherwise
				string GUID = Guid.NewGuid().ToString();
				currentPackage = new Package
				{
					Name = textBoxPackageName.Text,
					Author = textBoxPackageAuthor.Text,
					Description = textBoxPackageDescription.Text.Replace("\r\n", "\\r\\n"),
					//TODO:
					//Website = linkLabelPackageWebsite.Links[0],
					GUID = GUID,
					PackageVersion = new Version(0, 0, 0, 0),
					PackageType = newPackageType
				};
				textBoxGUID.Text = currentPackage.GUID;
				SaveFileNameButton.Enabled = true;
			}


		}


		private void dataGridViewReplacePackage_SelectionChanged(object sender, EventArgs e)
		{
			if (dataGridViewReplacePackage.SelectedRows.Count > 0)
			{
				switch (newPackageType)
				{
					case PackageType.Route:
						currentPackage = currentDatabase.InstalledRoutes[dataGridViewReplacePackage.SelectedRows[0].Index];
						currentPackage.PackageType = PackageType.Route;
						break;
					case PackageType.Train:
						currentPackage = currentDatabase.InstalledTrains[dataGridViewReplacePackage.SelectedRows[0].Index];
						currentPackage.PackageType = PackageType.Train;
						break;
					case PackageType.Other:
						currentPackage = currentDatabase.InstalledOther[dataGridViewReplacePackage.SelectedRows[0].Index];
						currentPackage.PackageType = PackageType.Other;
						break;
				}
			}
		}

		private void linkLabelPackageWebsite_Click(object sender, EventArgs e)
		{
			if (creatingPackage)
			{
				if (ShowInputDialog(ref currentPackage.Website) == DialogResult.OK)
				{
					linkLabelPackageWebsite.Text = currentPackage.Website;
					linkLabelPackageWebsite.Links[0].LinkData = currentPackage.Website;
				}
				
			}
			else
			{
				//Launch link in default web-browser
				if (linkLabelPackageWebsite.Links[0] != null)
				{
					Process.Start(currentPackage.Website);
				}
			}
		}

		private static DialogResult ShowInputDialog(ref string input)
		{
			System.Drawing.Size size = new System.Drawing.Size(200, 70);
			Form inputBox = new Form
			{
				FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
				ClientSize = size,
				Text = "Name"
			};


			System.Windows.Forms.TextBox textBox = new TextBox
			{
				Size = new System.Drawing.Size(size.Width - 10, 23),
				Location = new System.Drawing.Point(5, 5),
				Text = input
			};
			inputBox.Controls.Add(textBox);

			Button okButton = new Button
			{
				DialogResult = System.Windows.Forms.DialogResult.OK,
				Name = "okButton",
				Size = new System.Drawing.Size(75, 23),
				Text = "&OK",
				Location = new System.Drawing.Point(size.Width - 80 - 80, 39)
			};
			inputBox.Controls.Add(okButton);

			Button cancelButton = new Button
			{
				DialogResult = System.Windows.Forms.DialogResult.Cancel,
				Name = "cancelButton",
				Size = new System.Drawing.Size(75, 23),
				Text = "&Cancel",
				Location = new System.Drawing.Point(size.Width - 80, 39)
			};
			inputBox.Controls.Add(cancelButton);

			inputBox.AcceptButton = okButton;
			inputBox.CancelButton = cancelButton;
			inputBox.AcceptButton = okButton; 
			inputBox.CancelButton = cancelButton; 
			DialogResult result = inputBox.ShowDialog();
			input = textBox.Text;
			return result;
		}

		private static DialogResult ShowVersionDialog(ref Version minimumVersion, ref Version maximumVersion, string currentVersion)
		{
			System.Drawing.Size size = new System.Drawing.Size(200, 80);
			Form inputBox = new Form
			{
				FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
				ClientSize = size,
				Text = "Minimum Version"
			};


			System.Windows.Forms.TextBox textBox = new TextBox
			{
				Size = new System.Drawing.Size(size.Width - 10, 23),
				Location = new System.Drawing.Point(5, 5),
				Text = currentVersion
			};
			inputBox.Controls.Add(textBox);

			System.Windows.Forms.TextBox textBox2 = new TextBox();
			textBox2.Size = new System.Drawing.Size(size.Width - 10, 23);
			textBox2.Location = new System.Drawing.Point(5, 25);
			textBox2.Text = currentVersion;
			inputBox.Controls.Add(textBox2);

			Button okButton = new Button
			{
				DialogResult = System.Windows.Forms.DialogResult.OK,
				Name = "okButton",
				Size = new System.Drawing.Size(75, 23),
				Text = "&OK",
				Location = new System.Drawing.Point(size.Width - 80 - 80, 49)
			};
			inputBox.Controls.Add(okButton);

			Button cancelButton = new Button
			{
				DialogResult = System.Windows.Forms.DialogResult.Cancel,
				Name = "cancelButton",
				Size = new System.Drawing.Size(75, 23),
				Text = "&Cancel",
				Location = new System.Drawing.Point(size.Width - 80, 49)
			};
			inputBox.Controls.Add(cancelButton);

			inputBox.AcceptButton = okButton;
			inputBox.CancelButton = cancelButton;
			inputBox.AcceptButton = okButton;
			inputBox.CancelButton = cancelButton;
			DialogResult result = inputBox.ShowDialog();
			try
			{
				if (textBox.Text == String.Empty || textBox.Text == "0")
				{
					minimumVersion = null;
				}
				else
				{
					minimumVersion = Version.Parse(textBox.Text);	
				}
				if (textBox2.Text == String.Empty || textBox2.Text == "0")
				{
					minimumVersion = null;
				}
				else
				{
					maximumVersion = Version.Parse(textBox2.Text);
				}
			}
			catch
			{
				MessageBox.Show(Interface.GetInterfaceString("packages_creation_version_invalid"));
			}
			return result;
		}

		private void HidePanels()
		{
			panelPackageInstall.Hide();
			panelDependancyError.Hide();
			panelVersionError.Hide();
			panelSuccess.Hide();
			panelUninstallResult.Hide();
			panelPackageDependsAdd.Hide();
			panelCreatePackage.Hide();
			panelReplacePackage.Hide();
			panelPackageList.Hide();
			panelPleaseWait.Hide();
		}

		private void newPackageClearSelectionButton_Click(object sender, EventArgs e)
		{
			filesToPackage.Clear();
			filesToPackageBox.Clear();
		}

		//Don't use a file picker dialog- Select folders only.
		private void addPackageItemsButton_Click(object sender, EventArgs e)
		{
			var dialog = new CommonOpenFileDialog
			{
				AllowNonFileSystemItems = true,
				Multiselect = true,
				IsFolderPicker = true,
				EnsureFileExists = false
			};
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				var tempList = new List<PackageFile>();
				//This dialog is used elsewhere, so we'd better reset it's properties
				openPackageFileDialog.Multiselect = false;
				if (filesToPackage == null)
				{
					filesToPackage = new List<PackageFile>();
				}
				foreach (string rootPath in dialog.FileNames)
				{
					filesToPackageBox.Text += rootPath + Environment.NewLine;
					if (System.IO.File.Exists(rootPath))
					{
						//If this is an absolute path to a file, just add it to the list
						var File = new PackageFile
						{
							absolutePath = rootPath,
							relativePath = System.IO.Path.GetFileName(rootPath),
						};
						filesToPackage.Add(File);
						continue;
					}
					//Otherwise, this must be a directory- Get all the files within the directory and add to our list
					string[] files = System.IO.Directory.GetFiles(rootPath, "*.*", System.IO.SearchOption.AllDirectories);
					for (int i = 0; i < files.Length; i++)
					{
						var File = new PackageFile
						{
							absolutePath = files[i],
							relativePath = files[i].Replace(System.IO.Directory.GetParent(rootPath).ToString(), ""),
						};
						tempList.Add(File);
					}
				}
				filesToPackage.AddRange(DatabaseFunctions.FindFileLocations(tempList));
				
			}
		}

		private void replacePackageButton_Click(object sender, EventArgs e)
		{
			labelNewGUID.Text = Interface.GetInterfaceString("packages_creation_replace_id");
			textBoxGUID.Text = currentPackage.GUID;
			SaveFileNameButton.Enabled = true;
			panelReplacePackage.Hide();
			panelNewPackage.Show();
		}


		private void SaveFileNameButton_Click(object sender, EventArgs e)
		{
			savePackageDialog = new SaveFileDialog
			{
				Title = Interface.GetInterfaceString("packages_creation_save"),
				CheckPathExists = true,
				DefaultExt = "zip",
				Filter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
				FilterIndex = 2,
				RestoreDirectory = true
			};

			if (savePackageDialog.ShowDialog() == DialogResult.OK)
			{
				currentPackage.FileName = savePackageDialog.FileName;
				textBoxPackageFileName.Text = savePackageDialog.FileName;
			}
		}

		private void textBoxPackageFileName_TextChanged(object sender, EventArgs e)
		{
			currentPackage.PackageFile = textBoxPackageFileName.Text;
		}



		private void dataGridViewDependancies_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			string s = dataGridViewDependancies.SelectedCells[0].Value.ToString();
			//HACK: Multiple cells can be selected, so we should just check the first one
			if (s.StartsWith("www.") || s.StartsWith("http://"))
			{
				Process.Start(s);
			}
		}

		//This method resets the package installer to the default panels when clicking away, or when a creation/ install has finished
		private void ResetInstallerPanels()
		{
			HidePanels();
			panelPackageList.Show();
			creatingPackage = false;
			//Reset radio buttons in the installer
			radioButtonQ1Yes.Checked = false;
			radioButtonQ1No.Checked = false;
			radioButtonQ2Route.Checked = false;
			radioButtonQ2Train.Checked = false;
			radioButtonQ2Other.Checked = false;
			//Reset picturebox
			TryLoadImage(pictureBoxPackageImage, "route_unknown.png");
			TryLoadImage(pictureBoxProcessing, "logo.png");
			//Reset enabled boxes & panels
			textBoxGUID.Text = null;
			textBoxGUID.Enabled = false;
			SaveFileNameButton.Enabled = false;
			panelReplacePackage.Hide();
			panelNewPackage.Enabled = false;
			panelNewPackage.Show();
			//Set variables to uninitialised states
			creatingPackage = false;
			currentPackage = null;
			selectedTrainPackageIndex = 0;
			selectedRoutePackageIndex = 0;
			newPackageType = PackageType.NotFound;
			ImageFile = null;
			//Reset text
			textBoxPackageAuthor.Text = Interface.GetInterfaceString("packages_selection_none");
			textBoxPackageName.Text = Interface.GetInterfaceString("packages_selection_none");
			textBoxPackageDescription.Text = Interface.GetInterfaceString("packages_selection_none");
			textBoxPackageVersion.Text = Interface.GetInterfaceString("packages_selection_none");
			buttonSelectPackage.Text = Interface.GetInterfaceString("packages_install_select");
			labelNewGUID.Text = Interface.GetInterfaceString("packages_creation_new_id");
		}
	}
}
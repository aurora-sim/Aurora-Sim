using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class FrenchTranslation : ITranslator
    {
        public string LanguageName { get { return "fr"; } }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "Etat de la Grille";
                case "Online":
                    return "En Ligne";
                case "Offline":
                    return "Hors Ligne";
                case "TotalUserCount":
                    return "Nombre total d'utilisateurs";
                case "TotalRegionCount":
                    return "Nombre total de régions";
                case "UniqueVisitors":
                    return "Visiteurs unique (30 jours)";
                case "OnlineNow":
                    return "En ligne maintenant";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voix";
                case "Currency":
                    return "Monnaie";
                case "Disabled":
                    return "Désactivé";
                case "Enabled":
                    return "Activé";
                case "News":
                    return "Nouveautés";
                case "Region":
                    return "Région";
                case "Login":
                    return "Connection";
                case "UserName":
				    return "Nom d'utilisateur";
                case "UserNameText":
                    return "Nom d'utilisateur";
                case "Password":
				    return "Mot de passe";
                case "PasswordText":
                    return "Mot de passe";
                case "PasswordConfirmation":
                    return "Confirmer Mot de passe";
                case "ForgotPassword":
                    return "Mot de passe oublié?";
                case "Submit":
                    return "Envoyer";
					

                case "SpecialWindowTitleText":
                    return "Titre spécial de la fenêtre Info";
                case "SpecialWindowTextText":
                    return "Texte spécial de la fenêtre Infos";
                case "SpecialWindowColorText":
                    return "Couleur spécial de la fenêtre Infos";
                case "SpecialWindowStatusText":
                    return "Status spécial de la fenêtre Infos";
                case "WelcomeScreenManagerFor":
                    return "Welcome Screen Manager pour";
                case "ChangesSavedSuccessfully":
                    return "Changements enregistrés avec succès";

				
                case "AvatarNameText":
                    return "Nom de l'Avatar";
                case "AvatarScopeText":
                    return "Scope ID de l'Avatar";
                case "FirstNameText":
                    return "Votre Nom";
                case "LastNameText":
                    return "Votre Prénom";
                case "UserAddressText":
                    return "Votre Addresse";
                case "UserZipText":
                    return "Votre Code Zip";
                case "UserCityText":
                    return "Votre Ville";
                case "UserCountryText":
                    return "Votre Pays";
                case "UserDOBText":
                    return "Votre date d'anniversaire (Mois Jour Année)";
                case "UserEmailText":
                    return "Votre Email";
                case "RegistrationText":
                    return "Enregistrement de l'Avatar";
                case "RegistrationsDisabled":
                    return "Les inscriptions sont actuellement désactivés, s'il vous plaît réessayez à nouveau dans quelques temps...";
                case "TermsOfServiceText":
                    return "Conditions d'utilisation";
                case "TermsOfServiceAccept":
                    return "Acceptez-vous les Conditions d'utilisation détaillés ci-dessus?";
					
				// news
                case "OpenNewsManager":
                    return "Ouvrez le Gestionnaire des News";
                case "NewsManager":
                    return "Gestionnaire des News";
                case "EditNewsItem":
                    return "Editer  un Article News";
                case "AddNewsItem":
                    return "Ajouter une News";
                case "DeleteNewsItem":
                    return "Effacer une News";
                case "NewsDateText":
                    return "Date de la News";
                case "NewsTitleText":
                    return "Title de la News";
                case "NewsItemTitle":
                    return "Titre Article News";
                case "NewsItemText":
                    return "Texte Article News";
                case "AddNewsText":
                    return "Ajouter des News";
                case "DeleteNewsText":
                    return "Effacer des News";
                case "EditNewsText":
                    return "Editer des News";
                case "UserProfileFor":
                    return "Profil Utilisateur pour";
                case "ResidentSince":
                    return "Resident depuis";
                case "AccountType":
                    return "Type de compte";
                case "PartnersName":
                    return "Nom des Partenaires";
                case "AboutMe":
                    return "À propos de moi";
                case "IsOnlineText":
                    return "Status de l'Utilisateur";
                case "OnlineLocationText":
                    return "Emplacement actuelle de l'utilisateur";
					
                case "RegionInformationText":
                    return "Information sur la région";
                case "OwnerNameText":
                    return "Nom du propriétaire";
                case "RegionLocationText":
                    return "Emplacement de la Région";
                case "RegionSizeText":
                    return "Taille de la Région";
                case "RegionNameText":
                    return "Nom de la Région";
                case "RegionTypeText":
                    return "Type de Région";
                case "ParcelsInRegionText":
                    return "Parcelles dans la Région";
                case "ParcelNameText":
                    return "Nom de la Parcelle";
                case "ParcelOwnerText":
                    return "Nom des Propriétaires de Parcelles";

				// Region Page
                case "RegionInfoText":
                    return "Information de la Région";
                case "RegionListText":
                    return "Liste des Régions";
                case "RegionLocXText":
                    return "Région X";
                case "RegionLocYText":
                    return "Région Y";
                case "SortByLocX":
                    return "Trié par Région X";
                case "SortByLocY":
                    return "Trié par Région Y";
                case "SortByName":
                    return "Trié par Nom de Région";
                case "RegionMoreInfo":
                    return "Plus d'informations";
                case "RegionMoreInfoTooltips":
                    return "Plus d'informations au sujet de";
                case "FirstText":
                    return "Premier";
                case "BackText":
                    return "Précédent";
                case "NextText":
                    return "Suivant";
                case "LastText":
                    return "Dernier";
                case "CurrentPageText":
                    return "Page actuelle";
                case "MoreInfoText":
                    return "Plus d'informations";
                case "OnlineUsersText":
                    return "Utilisateurs en Ligne";
                case "RegionOnlineText":
                    return "Status de la Région";
                case "NumberOfUsersInRegionText":
                    return "Nombre d'Utilisateurs dans la Région";

				// Menu Buttons
                case "MenuHome":
                    return "Accueil";
                case "MenuLogin":
                    return "Connection";
                case "MenuLogout":
                    return "Déconnexion";
                case "MenuRegister":
                    return "Inscription";
                case "MenuForgotPass":
                    return "Mot de Passe Oublié";
                case "MenuNews":
                    return "News";
                case "MenuWorld":
                    return "Monde";
                case "MenuWorldMap":
                    return "Carte de Monde";
                case "MenuRegion":
                    return "Liste des Régions";
                case "MenuUser":
                    return "Utilisateurs";
                case "MenuOnlineUsers":
                    return "Utilisateurs en Ligne";
                case "MenuUserSearch":
                    return "Rechercher Utilisateur";
                case "MenuRegionSearch":
                    return "Rechercher Région";
                case "MenuChat":
                    return "Chat";
                case "MenuHelp":
                    return "Aide";
                case "MenuChangeUserInformation":
                    return "Modifier Infos Utilisateur";
                case "MenuWelcomeScreenManager":
                    return "Gestion Ecran Bienvenue";
                case "MenuNewsManager":
                    return "Gestion des News";
                case "MenuUserManager":
                    return "Gestion Utilisateurs";
                case "MenuFactoryReset":
                    return "Réinitialiser";
                case "ResetMenuInfoText":
                    return "Réinitialise les éléments de menu aux valeurs par défaut les plus à jour";
                case "ResetSettingsInfoText":
                    return "Réinitialise les réglages de l'interface Web aux valeurs par défaut les plus à jour";
                case "MenuPageManager":
                    return "Gestion des Pages";
                case "MenuSettingsManager":
                    return "Gestion des paramètres";
                case "MenuManager":
                    return "Admin";

				// Tooltips Menu Buttons
                case "TooltipsMenuHome":
                    return "Accueil";
                case "TooltipsMenuLogin":
                    return "Connection";
                case "TooltipsMenuLogout":
                    return "Déconnection";
                case "TooltipsMenuRegister":
                    return "Inscription";
                case "TooltipsMenuForgotPass":
                    return "Mot de Passe Oublié";
                case "TooltipsMenuNews":
                    return "News";
                case "TooltipsMenuWorld":
                    return "Monde";
                case "TooltipsMenuWorldMap":
                    return "Carte du Monde";
                case "TooltipsMenuRegion":
                    return "Liste des Régoins";
                case "TooltipsMenuUser":
                    return "Utilisateurs";
                case "TooltipsMenuOnlineUsers":
                    return "Utilisateurs en ligne";
                case "TooltipsMenuUserSearch":
                    return "Rechercher un Utilisateurs";
                case "TooltipsMenuRegionSearch":
                    return "Rechercher un Région";
                case "TooltipsMenuChat":
                    return "Chat";
                case "TooltipsMenuHelp":
                    return "Aide";
                case "TooltipsMenuChangeUserInformation":
                    return "Modifier les informations de l'utilisateur";
                case "TooltipsMenuWelcomeScreenManager":
                    return "Gestionnaire de l'Ecran de Bienvenue";
                case "TooltipsMenuNewsManager":
                    return "Gestionnaire des News";
                case "TooltipsMenuUserManager":
                    return "Gestionnaire des Utilisateurs";
                case "TooltipsMenuFactoryReset":
                    return "Réinitialiser";
                case "TooltipsMenuPageManager":
                    return "Gestionnaire de Pages";
                case "TooltipsMenuSettingsManager":
                    return "Gestionnaire de paramètres";
                case "TooltipsMenuManager":
                    return "Gestion Administrative";

				// Menu Region
                case "MenuRegionTitle":
                    return "Region";
                case "MenuParcelTitle":
                    return "Parcel";
                case "MenuOwnerTitle":
                    return "Owner";
				
				// Menu Profile
                case "MenuProfileTitle":
                    return "Profil";
                case "MenuGroupTitle":
                    return "Group";
                case "MenuPicksTitle":
                    return "Picks";

				// Urls
                case "WelcomeScreen":
                    return "Ecran de Bienvenue";
				
				// Tooltips Urls
                case "TooltipsWelcomeScreen":
                    return "Ecran de Bienvenue";
                case "TooltipsWorldMap":
                    return "Carte du Monde";

				// Style Switcher
                case "styles1":
                    return "Defaut Minimaliste";
                case "styles2":
                    return "Dégardé Clair";
                case "styles3":
                    return "Bleu Nuit";
                case "styles4":
                    return "Dégradé Foncé";
                case "styles5":
                    return "Luminus";

                case "StyleSwitcherStylesText":
                    return "Styles";
                case "StyleSwitcherLanguagesText":
                    return "Langages";
                case "StyleSwitcherChoiceText":
                    return "Choix";

                // Language Switcher Tooltips
                case "en":
                    return "Anglais";
                case "fr":
                    return "Français";
                case "de":
                    return "Allemand";
                case "it":
                    return "Italien";
                case "es":
                    return "Espagnol";

				// Index Page
                case "HomeText":
                    return "Accueil";
                case "HomeTextWelcome":
                    return "Ceci est notre Nouveau Monde Virtuel! Rejoignez-nous dés maintenant, et faites la différence!";
                case "HomeTextTips":
                    return "Nouvelles présentations";
                case "WelcomeToText":
                    return "Bienvenue";

				// World Map Page
                case "WorldMap":
                    return "Carte du Monde";
                case "WorldMapText":
                    return "Plein Ecran";

				// Chat Page
                case "ChatText":
                    return "Chat de Support";
					
                // Help Page
                case "HelpText":
                    return "Aide";
                case "HelpViewersConfigText":
                    return "Aide pour la configuration des Viewers (Clients)";
                case "AngstormViewer":
                    return "Angstorm Viewer (Client)";
                case "VoodooViewer":
                    return "Voodoo Viewer (Client)";
                case "AstraViewer":
                    return "Astra Viewer (Client)";
                case "ImprudenceViewer":
                    return "Imprudence Viewer (Client)";
                case "PhoenixViewer":
                    return "Phoenix Viewer (Client)";
                case "SingularityViewer":
                    return "Singularity Viewer (Client)";
                case "ZenViewer":
                    return "Zen Viewer (Client)";
				
                //Logout page
                case "LoggedOutSuccessfullyText":
                    return "Vous avez été déconnecté avec succès.";
                case "Logout":
                    return "Déconnection";
					
                //Change user information page
                case "ChangeUserInformationText":
                    return "Modifier les informations de l'utilisateur";
                case "ChangePasswordText":
                    return "Changer de Mot de Passe";
                case "NewPasswordText":
                    return "Nouveau Mot de Passe";
                case "NewPasswordConfirmationText":
                    return "Nouveau Mot de Passe (Confirmation)";
                case "ChangeEmailText":
                    return "Changer d'Adresse Email";
                case "NewEmailText":
                    return "Nouvelle Adresse Email";
                case "DeleteUserText":
                    return "Effacer mon Compte";
                case "DeleteText":
                    return "Effacer";
                case "DeleteUserInfoText":
                    return "Cela permettra d'éliminer toutes les informations vous concernant dans la grille et retirer votre accès à ce service. Si vous souhaitez continuer, saisissez votre nom et mot de passe et cliquez sur Supprimer.";
                case "EditText":
                    return "Editer";
                case "EditUserAccountText":
                    return "Modifier un compte utilisateur";

                //Maintenance page
                case "WebsiteDownInfoText":
                    return "Le Site Web est actuellement en panne, s'il vous plaît réessayez ultérieurement...";
                case "WebsiteDownText":
                    return "Site Web Hors Ligne";
					
                //http_404 page
                case "Error404Text":
                    return "Code d'Erreur";
                case "Error404InfoText":
                    return "404 La page n'a pu être trouvée";
                case "HomePage404Text":
                    return "Page d'Accueil";
					
                //http_505 page
                case "Error505Text":
                    return "Code d'Erreur";
                case "Error505InfoText":
                    return "505 Erreur Interne du Server";
                case "HomePage505Text":
                    return "Page d'Accueil";

                //user_search page
                case "Search":
                    return "Rechercher";
                case "SearchText":
                    return "Rechercher";
                case "SearchForUserText":
                    return "Recherche par Utilisateur";
                case "UserSearchText":
                    return "Rechercher un Utilisateur";
                case "SearchResultForUserText":
                    return "Résultat de la Recherche pour l'Utilisateur";

                //region_search page
                case "SearchForRegionText":
                    return "Rechercher une Région";
                case "RegionSearchText":
                    return "Rechercher une Région";
                case "SearchResultForRegionText":
                    return "Résultat de recherche de la région";

                //Edit user page
                case "AdminDeleteUserText":
                    return "Supprimer l'utilisateur";
                case "AdminDeleteUserInfoText":
                    return "Cette commande supprime le compte et détruit toutes les informations qui lui sont associés.";
                case "BanText":
                    return "Bannir";
                case "UnbanText":
                    return "Débannir";
                case "AdminTempBanUserText":
                    return "Temps de Bannissement";
                case "AdminTempBanUserInfoText":
                    return "Cela empêche l'utilisateur de se connecter pour un laps de temps.";
                case "AdminBanUserText":
                    return "Bannir l'utilisateur";
                case "AdminBanUserInfoText":
                    return "Cela empêche l'utilisateur de se connecter tant que l'interdiction n'est pas supprimée.";
                case "AdminUnbanUserText":
                    return "Débannir un Utilisateur";
                case "AdminUnbanUserInfoText":
                    return "Supprime les interdictions temporaires et permanentes sur l'utilisateur.";
                case "AdminLoginInAsUserText":
                    return "Connectez-vous en tant qu'utilisateur";
	            case "AdminLoginInAsUserInfoText":
                    return "Vous serez déconnecté de votre compte admin, et connecté en tant que cet utilisateur, et vous verrez tout comme ils le voient.";
                case "TimeUntilUnbannedText":
                    return "Temps jusqu'à la levée du Bannissement de l'utilisateur";
                case "DaysText":
                    return "Jours";
                case "HoursText":
                    return "Heures";
                case "MinutesText":
                    return "Minutes";
                case "EdittingText":
                    return "Edition";
                case "BannedUntilText":
                    return "L'utilisateur est interdit jusqu'à ce que";
                case "KickAUserText":
                    return "Kicker l'utilisateur (L'utilisateur sera déconnecté dans les 30 secondes)";
                case "KickMessageText":
                    return "Message à l'utilisateur";
                case "KickUserText":
                    return "Kicker l'utilisateur";

                //factory_reset
                case "FactoryReset":
                    return "Réinitialiser";
                case "ResetMenuText":
                    return "Réinitialiser les paramètres par défaut du menu";
                case "ResetSettingsText":
                    return "Rétablir les paramètres Web (page Gestionnaire de paramètres) par défaut";
                case "Reset":
                    return "Réinitialiser";
                case "Settings":
                    return "Paramètres";
                case "Pages":
                    return "Pages";
                case "DefaultsUpdated":
                    return "Mise à jour par défaut, rendez-vous sur \"Réinitialiseré\" ou \"Gestionnaire de paramètres\" pour désactiver cet avertissement.";

                //page_manager
                case "PageManager":
                    return "Gestionnaire de Pages";
                case "SaveMenuItemChanges":
                    return "Enregistrer un élément de menu";
                case "SelectItem":
                    return "Selectionner un élément";
                case "DeleteItem":
                    return "Effacer un élément";
                case "AddItem":
                    return "Ajouter un élément";
                case "PageLocationText":
                    return "Emplacement de la page";
                case "PageIDText":
                    return "ID de la Page";
                case "PagePositionText":
                    return "Position de la Page";
                case "PageTooltipText":
                    return "Tooltip de la Page";
                case "PageTitleText":
                    return "Titre de la Page";
                case "No":
                    return "Non";
                case "Yes":
                    return "Oui";
                case "RequiresLoginText":
                    return "Vous devez vous connecter pour voir";
                case "RequiresLogoutText":
                    return "Vous devez vous déconnecter pour voir";
                case "RequiresAdminText":
                    return "Vous devez vous connecter en temps qu'Admin pour voir";

                //settings manager page
                case "Save":
                    return "Sauver";
                case "GridCenterXText":
                    return "Grille Location Centrer X";
                case "GridCenterYText":
                    return "Grille Location Centrer Y";
                case "GoogleMapAPIKeyText":
                    return "Google Maps API Key";
                case "GoogleMapAPIKeyHelpText":
                    return "Générer la Google Maps API KEY v2 ici";
                case "SettingsManager":
                    return "Gestionnaire de paramètres";
                case "IgnorePagesUpdatesText":
                    return "Ignorer les avertissements de mises à jour des pages jusqu'à la prochaine mise à jour";
                case "IgnoreSettingsUpdatesText":
                    return "Ignorer les avertissements de mises à jour des paramètres jusqu'à la prochaine mise à jour";

                //Times
                case "Sun":
                    return "Dim";
                case "Mon":
                    return "Lun";
                case "Tue":
                    return "Mar";
                case "Wed":
                    return "Mer";
                case "Thu":
                    return "Jeu";
                case "Fri":
                    return "Ven";
                case "Sat":
                    return "Sam";
                case "Sunday":
                    return "Dimanche";
                case "Monday":
                    return "Lundi";
                case "Tuesday":
                    return "Mardi";
                case "Wednesday":
                    return "Mercredi";
                case "Thursday":
                    return "Jeudi";
                case "Friday":
                    return "Vendredi";
                case "Saturday":
                    return "Samedi";

                case "Jan_Short":
                    return "Jan";
                case "Feb_Short":
                    return "Fev";
                case "Mar_Short":
                    return "Mar";
                case "Apr_Short":
                    return "Avr";
                case "May_Short":
                    return "Mai";
                case "Jun_Short":
                    return "Jun";
                case "Jul_Short":
                    return "Jui";
                case "Aug_Short":
                    return "Aou";
                case "Sep_Short":
                    return "Sep";
                case "Oct_Short":
                    return "Oct";
                case "Nov_Short":
                    return "Nov";
                case "Dec_Short":
                    return "Dec";

                case "January":
                    return "Janvier";
                case "February":
                    return "Février";
                case "March":
                    return "Mars";
                case "April":
                    return "Avril";
                case "May":
                    return "Mai";
                case "June":
                    return "Juin";
                case "July":
                    return "Juillet";
                case "August":
                    return "Août";
                case "September":
                    return "Septembre";
                case "October":
                    return "Octobre";
                case "November":
                    return "Novembre";
                case "December":
                    return "Decembre";
					
				// ColorBox
                case "ColorBoxImageText":
                    return "Image";
                case "ColorBoxOfText":
                    return "sur";
                case "ColorBoxPreviousText":
                    return "Précédent";
                case "ColorBoxNextText":
                    return "Suivant";
                case "ColorBoxCloseText":
                    return "Fermer";
                case "ColorBoxStartSlideshowText":
                    return "Démarrer Slide Show";
                case "ColorBoxStopSlideshowText":
                    return "Arrêter Slide Show";
	
            }
            return "UNKNOWN CHARACTER";
        }
    }
}

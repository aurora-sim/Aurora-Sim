$(document).ready(function(){
	//References
	var sections = $("#menu li");
	var loading = $("#loading");
	var content = $("#content");
	
	//Manage click events
	sections.click(function(){
		//show the loading bar
		showLoading();
		//load selected section
		switch(this.id){
			case "home":
				content.slideUp();
				content.load("index.html #content", hideLoading);
				content.slideDown();
				break;
			case "login":
				content.slideUp();
				content.load("login.html", hideLoading);
				content.slideDown();
				break;
			case "register":
				content.slideUp();
				content.load("register.html", hideLoading);
				content.slideDown();
				break;
			case "news":
				content.slideUp();
				content.load("news_list.html", hideLoading);
				content.slideDown();
				break;
			case "world":
				content.slideUp();
				content.load("world.html", hideLoading);
				content.slideDown();
				break;
			case "agent_info":
				content.slideUp();
				content.load("agent_info.html", hideLoading);
				content.slideDown();
				break;
			case "online_users":
				content.slideUp();
				content.load("online_users.html", hideLoading);
				content.slideDown();
				break;
			case "region_info":
				content.slideUp();
				content.load("region_info.html", hideLoading);
				content.slideDown();
				break;
			case "region_list":
				content.slideUp();
				content.load("region_list.html", hideLoading);
				content.slideDown();
				break;
			case "chat":
				content.slideUp();
				content.load("chat.html", hideLoading);
				content.slideDown();
				break;
			case "tweets":
				content.slideUp();
				content.load("tweets.html", hideLoading);
				content.slideDown();
				break;
			case "help":
				content.slideUp();
				content.load("help.html", hideLoading);
				content.slideDown();
				break;
			default:
				//hide loading bar if there is no selected section
				hideLoading();
				break;
		}
	});

	//show loading bar
	function showLoading(){
		loading
			.css({visibility:"visible"})
			.css({opacity:"1"})
			.css({display:"block"})
		;
	}
	//hide loading bar
	function hideLoading(){
		loading.fadeTo(1000, 0);
	};
});
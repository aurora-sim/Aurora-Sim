$(document).ready(function(){
	//References
	var sections = $("#menu li");
	var loading = $("#loading");
	var content = $("#content");
	
	//Manage click events
	sections.click(function(event){
		//show the loading bar
		showLoading();
		//load selected section
		switch(this.id){
{MenuItemsArrayBegin}
			case "{MenuItemID}":
				content.slideUp('swing',  function() { 
				    content.load("{MenuItemLocation}" + window.location.search, hideLoading);
				    content.slideDown();
				});
				
				break;
{MenuItemsArrayEnd}
			default:
				//hide loading bar if there is no selected section
				hideLoading();
				break;
		}
		event.stopPropagation();
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
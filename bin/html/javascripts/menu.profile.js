$(document).ready(function(){
	//References
	var sections = $("#menuprofile li");
	var loading = $("#loading");
	var content = $("#content");

	//Manage click events
	sections.click(function(){
		//show the loading bar
		showLoading();
		//load selected section
		switch(this.id){
			case "base":
				content.slideUp(  function() {
				content.load("base.html" + window.location.search, hideLoading);
				content.slideDown();
				});
				break;
			case "groups":
				content.slideUp(  function() {
				content.load("groups.html" + window.location.search, hideLoading);
				content.slideDown();
				});
				break;
			case "picks":
				content.slideUp(  function() {
				content.load("picks.html" + window.location.search, hideLoading);
				content.slideDown();
				});
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
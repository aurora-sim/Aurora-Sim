package aurora.webui.client;

import com.google.gwt.core.client.EntryPoint;
import com.google.gwt.core.client.GWT;
import com.google.gwt.event.logical.shared.ResizeEvent;
import com.google.gwt.event.logical.shared.ResizeHandler;
import com.google.gwt.http.client.Request;
import com.google.gwt.http.client.RequestBuilder;
import com.google.gwt.http.client.RequestCallback;
import com.google.gwt.http.client.RequestException;
import com.google.gwt.http.client.Response;
import com.google.gwt.http.client.URL;
import com.google.gwt.user.client.Window;
import com.google.gwt.user.client.ui.Image;
import com.google.gwt.user.client.ui.Label;
import com.google.gwt.user.client.ui.RootPanel;
import com.smartgwt.client.types.Alignment;
import com.smartgwt.client.types.Side;
import com.smartgwt.client.types.VerticalAlignment;
import com.smartgwt.client.widgets.IButton;
import com.smartgwt.client.widgets.events.ClickEvent;
import com.smartgwt.client.widgets.events.ClickHandler;
import com.smartgwt.client.widgets.layout.HLayout;
import com.smartgwt.client.widgets.layout.VLayout;
import com.smartgwt.client.widgets.tab.Tab;
import com.smartgwt.client.widgets.tab.TabSet;
import com.smartgwt.client.widgets.tab.events.CloseClickHandler;
import com.smartgwt.client.widgets.tab.events.TabCloseClickEvent;

/**
 * Entry point classes define <code>onModuleLoad()</code>.
 */
public class Aurora_WebUI implements EntryPoint {
	/**
	 * The message displayed to the user when the server cannot be reached or
	 * returns an error.
	 */
	private static final String SERVER_ERROR = "An error occurred while "
			+ "attempting to contact the server. Please check your network "
			+ "connection and try again.";

	/**
	 * This is the entry point method.
	 */
	public void onModuleLoad() {
		
		VLayout mainPanel = new VLayout();
		mainPanel.setWidth(800);
		mainPanel.setHeight(600);
		mainPanel.addMember(new Image(GWT.getHostPageBaseURL()+"images/aurora_logo.png"));		
		
		
		final TabSet topTabSet = new TabSet();  
        topTabSet.setTabBarPosition(Side.TOP);  
        topTabSet.setTabBarAlign(Side.LEFT);  
        topTabSet.setWidth(800);  
        topTabSet.setHeight(600);  
  
        Tab tTab1 = new Tab("Home");  
    

		final Label errorText = new Label("Error will show here if there is one");
        VLayout homePanel = new VLayout();
        final Label activeRegionsLabel = new Label("Active Regions: None");
        homePanel.addMember(activeRegionsLabel);
        final String url = GWT.getHostPageBaseURL()+"Aurora/ActiveRegions";
		RequestBuilder builder = new RequestBuilder(RequestBuilder.GET, URL.encode(url));

		try {
		  builder.sendRequest(null, new RequestCallback() {
		    public void onError(Request request, Throwable exception) {
		    	// Couldn't connect to server (could be timeout, SOP violation, etc.)    
		    	errorText.setText("There was an unknown problem while getting active regions.");     
		    }

			@Override
			public void onResponseReceived(Request request, Response response) {
				if (200 == response.getStatusCode()) {
					activeRegionsLabel.setText("Active Regions:"+response.getText());
				} else {
					// Handle the error.  Can get the status text from response.getStatusText()
					errorText.setText("There was a problem while getting active regions."+url);
				}
			}       
		  });
		} catch (RequestException e) {
			//could not connect to server
			errorText.setText(SERVER_ERROR);
		}
                
        IButton resetButton = new IButton("Restart all sims");
        homePanel.addMember(resetButton);
		homePanel.addMember(errorText);
		
		// Add a handler to close the DialogBox
		resetButton.addClickHandler(new ClickHandler()
		{
			@Override
			public void onClick(
					com.smartgwt.client.widgets.events.ClickEvent event) {
				final String url = GWT.getHostPageBaseURL()+"Aurora/Reset";
				RequestBuilder builder = new RequestBuilder(RequestBuilder.GET, URL.encode(url));

				try {
				  builder.sendRequest(null, new RequestCallback() {
				    public void onError(Request request, Throwable exception) {
				    	// Couldn't connect to server (could be timeout, SOP violation, etc.)    
				    	errorText.setText("There was an unknown problem while resetting.");     
				    }

					@Override
					public void onResponseReceived(Request request, Response response) {
						if (200 == response.getStatusCode()) {
							Window.alert(response.getText());
						} else {
							// Handle the error.  Can get the status text from response.getStatusText()
							errorText.setText("There was a problem while resetting."+url);
						}
					}       
				  });
				} catch (RequestException e) {
					//could not connect to server
					errorText.setText(SERVER_ERROR);
				}
			}
		});
        
        tTab1.setPane(homePanel);  
  
        Tab tTab2 = new Tab("Region Operations");
        tTab2.setCanClose(true);
        tTab2.setPane(new IButton("More specific stuff will go in secondary tabs"));  
  
        topTabSet.addTab(tTab1);  
        topTabSet.addTab(tTab2);  
  
        topTabSet.addCloseClickHandler(new CloseClickHandler() {  
            public void onCloseClick(TabCloseClickEvent event) {  
                Tab tab = event.getTab();  
                System.out.println("");  
            }  
        });  
  
		mainPanel.addMember(topTabSet);
		
		
		
		final HLayout  vp = new HLayout ();
		vp.setAlign(Alignment.CENTER);
		vp.addMember(mainPanel);
		vp.setBackgroundColor("black");
		vp.setBackgroundImage(GWT.getHostPageBaseURL()+"images/aurora_background.png");
		vp.setWidth("100%");
		vp.setHeight(Window.getClientHeight() + "px");
		Window.addResizeHandler(new ResizeHandler() {
		@Override
		public void onResize(ResizeEvent event) {
			int height = event.getHeight();
		    vp.setHeight(height + "px");
		}
		});
		RootPanel.get().add(vp);
	}
}

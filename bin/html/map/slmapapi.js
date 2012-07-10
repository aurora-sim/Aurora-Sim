/**
* @license License and Terms of Use
*
* Copyright (c) 2010 Linden Research, Inc.
* Copyright (c) 2011 SignpostMarv
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*
* This javascript makes use of the Second Life Map API, which is documented
* at http://wiki.secondlife.com/wiki/Map_API 
*
* Use of the Second Life Map API is subject to the Second Life API Terms of Use:
*   https://wiki.secondlife.com/wiki/Linden_Lab_Official:API_Terms_of_Use
*
* Questions regarding this javascript, and any suggested improvements to it, 
* should be sent to the mailing list opensource-dev@list.secondlife.com
*/
//
//  Map of how SL map relates to the Google map:
//
//  ( 0, 0) Google map pixels
//  (90, 0) lat, long
//  (0, SLURL.gridEdgeSizeInRegions) grid_x, grid_y
//  /\
//   |------------------------------------+
//   |                                    |
//   |                                    |
//   |                                    |
//   |                                    |
//   |                                    |
//   |                                    |
//   |                                    |
//   |                                    |
//   | xxx                                |
//   | xxx                                |       (big, big) Google map pixels
//   ------------------------------------------>  (0,90) lat, long
//                                                (SLURL.gridEdgeSizeInRegions, 0) grid_x, grid_y
//
//  As this map shows, SL is mapped to the upper right quadrant of the
//  'world' map as used by Google lat/long coordinates.  A large scaling value
//  called SLURL.gridEdgeSizeInRegions is used to set the largest region coordinate
//  at the top and far right edge of the displayed area.  At the current
//  value of 2^20 = 1M, this creates a map area with room for 1 trillion sims.
//  The little xxx's in the diagram shows where the populated sims are in SL
//  today.
//
//

//
// Taken from prototype.js...
//

Object.extend = function(destination, source) 
{
	for (property in source) 
	{
			destination[property] = source[property];
	}

	return destination;
}

//
// ...end of prototype.js functions
//

// SLURL namespace
var SLURL = {
// SL map tile widths and heights are equal, so there's only one constant for tile size
	tileSize                   : 256.0,

// The maximum width/height of the SL grid in regions:
// 2^20 regions on a side = 1,048,786  ("This should be enough for anyone")
// *NOTE: This must be a power of 2 and divisible by 2^(max zoom) = 256
	gridEdgeSizeInRegions      : 1048576,

// We map a 1,048,576 (2^20) regions-per-side square positioned at the origin onto Lat/Long (0, 0) to (-90, 90)
	mapFactor                  : 90.0 / 1048576,

// Max/min zoom levels for SL maps (they are mapped to GMap zoom levels in a centralised place)
	minZoomLevel               : 8, // Zoomed out as much as possible
	maxZoomLevel               : 1, // Zoomed in as much as possible

// Delay for mouse hover action (mouse has to be still for this many milliseconds)
	mouseHoverDelay            : 1000,

// Do we want to display hover information for the map?
	showHoverTips              : false,

// color when no region tile available
	backgroundColor            : '#1D475F',

// Provides debugging information if enabled
	debugMode                  : false,

// this is to be used if we want to be paranoid about case-sensitivity in region names
	paranoidMode               : false,

// To allow for asynchronous access to the slurl.com APIs, we need to have a work around that allows us to assign variables in the global window scope
	getRegionCoordsByNameQueue : 0, // simple increment, rather than using randomly generated numbers
	getRegionCoordsByNameVar   : function(){ // returns a variable name more-or-less guaranteed to be unoccupied by any other API call
		return 'slurlGetRegionCoordsByName_' + (++SLURL.getRegionCoordsByNameQueue);
	},
	getRegionCoordsByName      : function(region, onLoadHandler, variable){
		variable = variable || 'slRegionPos_result'; // if no variable is specified, assign a default
		SLURL.loadScript(
			'{WorldMapServiceAPIURL}/get-region-coords-by-name?var=' + encodeURIComponent(variable) + '&sim_name=' + encodeURIComponent(region),
			function(){
				onLoadHandler(window[variable]);
			}
		);
	},
	getRegionNameByCoordsQueue : 0,
	getRegionNameByCoordsVar   : function(){
		return 'slurlGetRegionNameByCoords_' + (++SLURL.getRegionNameByCoordsQueue);
	},
	getRegionNameByCoords      : function(x, y, onLoadHandler, variable){
		variable = variable || 'slRegionName';
		SLURL.loadScript(
			'{WorldMapAPIServiceURL}/get-region-name-by-coords?var=' + encodeURIComponent(variable) + '&grid_x=' + encodeURIComponent(x) + '&grid_y=' + encodeURIComponent(y),
			function(){
				onLoadHandler(window[variable]);
			}
		);
	},

	gotoSLURL                  : function(slMap, region, x, y){ // two modes of use: SLURL.gotoSLURL(map instance, region name, local x coordinate, local y coordinate) and SLURL.gotoSLURL(map instance, grid x coordinate, grid y coordinate)
		if(typeof region == 'number'){ // if region is a number, then we're operating in the second mode so we reassign the variables appropriately
			y = x;
			x = region;
			region = undefined;
		}
		function mapWindow(regionName, gridX, gridY){
			var
				url       = ['secondlife://' + encodeURIComponent(regionName), (gridX % 1) * 256, (gridY % 1) * 256].join('/'),
				debugInfo = SLURL.debugMode ? ' x: ' + Math.floor(gridX) + ' y: ' + Math.floor(gridY) : '';
			;
			slMap.addMapWindow( new SLURL.MapWindow('<b>' + regionName + '</b><br>' + debugInfo + '<a href="' + url + '" class="teleport-button">Teleport Now</a>'), new SLURL.XYPoint(gridX, gridY));
		}
		if(region == undefined){
			SLURL.getRegionNameByCoords(Math.floor(x), Math.floor(y), function(result){
				if(typeof result == 'string'){
					mapWindow(result, x, y);
				}else if((result == null || result.error) && SLURL.debugMode){
					alert('The coordinates of the SLURL (' + x + ', ' + y + ') were not recognised as being in a SecondLife region.');
				}
			}, SLURL.getRegionCoordsByNameVar());
		}else{
			x = x || 128;
			y = y || 128;
			SLURL.getRegionCoordsByName(region, function(result){
				if(result.x && result.y){
					x = result.x + (x / 256);
					y = result.y + (y / 256);
					if(SLURL.paranoidMode){
						SLURL.gotoSLURL(slMap, x, y);
					}else{
						mapWindow(region, x, y);
					}
				}else if(result.error && SLURL.debugMode){
					alert('No coordinates could be found for region "' + region + '"');
				}
			}, SLURL.getRegionNameByCoordsVar());
		}
	},

	loadScript                 : function(scriptURL, onLoadHandler){
		var script  = document.createElement('script');
		script.src  = scriptURL;
		script.type = 'text/javascript';

		if(onLoadHandler){ // Install the specified onload handler
			script.onload = onLoadHandler;  // Standard onload for Firefox/Safari/Opera etc
			script.onreadystatechange = function(){ // Need to use ready state change for IE as it doesn't support onload for scripts
				if(script.readyState == 'complete' || script.readyState == 'loaded'){
					onLoadHandler();
				}
			}
		}

		document.body.appendChild(script);
	},

//  This Function returns the appropriate image tile from the S3 storage site corresponding to the
//  input location and zoom level on the google map.
	getTileUrl                 : function(pos, zoom){
		var sl_zoom = SLURL.convertZoom(zoom);

		var regions_per_tile_edge = Math.pow(2, sl_zoom - 1);
		
		var x = pos.x * regions_per_tile_edge;
		var y = pos.y * regions_per_tile_edge;

		// Adjust Y axis flip location by zoom value, in case the size of the whole
		// world is not divisible by powers-of-two.
		var offset = SLURL.gridEdgeSizeInRegions;
		offset -= offset % regions_per_tile_edge;
		y = offset - y;

		// Google tiles are named (numbered) based on top-left corner, but ours
		// are named by lower-left corner.  Since we flipped in Y, correct the
		// name.  JC
		y -= regions_per_tile_edge;
		
		// We name our tiles based on the grid_x, grid_y of the region in the
		// lower-left corner.
		x -= x % regions_per_tile_edge;
		y -= y % regions_per_tile_edge; 

		return (
			[ // this used to be a variable, but it wasn't used anywhere else in the JS, so it was moved here
//
//  Add 2 hosts so that we get faster performance on clients with lots
//  of bandwidth but possible browser limits on number of open files
//
				"{WorldMapServiceURL}",
				"{WorldMapServiceURL}"
			][((x / regions_per_tile_edge) % 2)] //  Pick a server
			+ ["/map", sl_zoom, x, y, "objects.jpg"].join("-") //  Get image tiles from Amazon S3
		);
	},

// We map SL zoom levels to farthest out zoom levels for GMaps, as the Zoom control will then
// remove ticks for any zoom levels higher than we allow. (We map it in this way because it doesn't
// do the same for zoom levels lower than we allow).
	convertZoom                : function(zoom){
		return 8 - zoom;
	},

// Represents grid coordinates, equivalent LSL: integer pos = (llGetRegionCorner() / 256);
	XYPoint                    : function(x,y){
		this.x = x;
		this.y = y;
	},

// Represents named region coordinate with local region coordinate offset
	RegionPoint                : function(regionName, localX, localY){
		var obj = this;
		SLURL.getRegionCoordsByName(regionName, function(result){
			if(SLURL.debugMode){
				if(result == undefined){
					alert('API query for region coordinates failed');
				}else if(result.error){
					alert('API query returned an error');
				}
			}else if(typeof result == 'object'){
				if(result.x && result.y){
					obj.x = result.x + (Math.min(Math.max(localX, 0), 256) / 256);
					obj.y = result.y + (Math.min(Math.max(localY, 0), 256) / 256);
					obj.found = true;
				}
			}
		}, SLURL.getRegionCoordsByNameVar());
	},

// Represents an area of the grid
	Bounds                     : function(xMin, xMax, yMin, yMax){
		this.xMin = xMin || 0;
		this.xMax = xMax || 0;
		this.yMin = yMin || 0;
		this.yMax = yMax || 0;
	},

// Create the Euclidean Projection for the flat map
	EuclideanProjection        : function(NumZoomLevels){
		this.pixelsPerLonDegree=[];
		this.pixelsPerLonRadian=[];
		this.pixelOrigo=[];
		this.tileBounds=[];
		var BitmapSize = 512;
		var c=1;
		
		for(var d=0; d < NumZoomLevels; d++){
			var e= BitmapSize / 2;
			this.pixelsPerLonDegree.push(BitmapSize / 360);
			this.pixelsPerLonRadian.push(BitmapSize / (2*Math.PI));
			this.pixelOrigo.push(new GPoint(e,e));
			this.tileBounds.push(c);
			BitmapSize *= 2;
			c*=2
		}
	},

// Img
	Img                        : function(imgURL, imgWidth, imgHeight, hasAlpha){		
		this.URL    = imgURL;
		this.width  = imgWidth;
		this.height = imgHeight;
		this.alpha  = !!hasAlpha; // double inversion converts to boolean regardless of input
	},

// Icon
	Icon                       : function(imageMain, imageShadow){
		this.mainImg=imageMain;
		if(imageShadow){
			this.shadowImg = imageShadow;
		}
	},

// ------------------------------------
//
//              Marker
//
// ------------------------------------
	Marker                     : function(icons, pos, options){
		this.icons   = icons;
		this.slCoord = pos;
		this.options = new MarkerOptions(options);
	},
	MarkerOptions              : function(options){
		this.clickHandler       = false;
		this.onMouseOverHandler = false;
		this.onMouseOutHandler  = false;
		this.centerOnClick      = false;
		this.autopanOnClick     = true;
		this.autopanPadding     = 45;
		this.verticalAlign      = "middle";
		this.horizontalAlign    = "center";
		this.zLayer             = 0;
		if(options){
			Object.extend(this, options);
		}
	},

// MapWindow
	MapWindow                  : function(text, options){
		this.text    = text;
		this.options = options;
	},

// ------------------------------------
//
//            SLMapOptions
//
// ------------------------------------
	MapOptions                 : function(options){
		this.hasZoomControls            = true;
		this.hasPanningControls         = true;
		this.hasOverviewMapControl      = true;
		this.onStateChangedClickHandler = null;
		
		if(options){
			Object.extend(this, options);
		}

		this.zoomMin = Math.min(this.zoomMin, SLURL.minZoomLevel);
		this.zoomMax = Math.max(this.zoomMax, SLURL.maxZoomLevel);
	},

// ------------------------------------
//
//               SLMap
//
// ------------------------------------
	Map                        : function(map_element, map_options){
		var slMap = this;
		if (GBrowserIsCompatible()){
			slMap.ID                 = null;
			slMap.showingHoverWindow = false;
			slMap.options            = new SLURL.MapOptions(map_options);
			slMap.mapProjection      = new SLURL.EuclideanProjection(18);

			// Create our custom map types and initialise map with them
			var
				mapTypes           = slMap.CreateMapTypes(),
				mapDiv             = slMap.CreateMapDiv(map_element),
				mapOpts            = {
					"mapTypes"        : mapTypes,
					"backgroundColor" : SLURL.backgroundColor
				},
				addZoomControls    = true,
				addPanControls     = true,
				overviewMapControl = true
			;

			slMap.GMap             = new GMap2(mapDiv, mapOpts);
			slMap.GMap.slMap       = slMap; // Link GMap back to us
			slMap.currentMapWindow = null; // No GMap info windows open yet
			slMap.voiceMarkers     = []; // No voice markers yet

			if (slMap.options){
				addPanControls     = !!slMap.options.hasPanningControls;
				addZoomControls    = !!slMap.options.hasZoomControls;
				overviewMapControl = !!slMap.options.hasOverviewMapControl;
			}

			if (addZoomControls || addPanControls){ // Use GMaps native controls
				slMap.GMap.addControl(new GSmallMapControl());
			}

			if (overviewMapControl){
				slMap.GMap.addControl(new GOverviewMapControl());
			}

			// Use GMaps xtra control methods
			slMap.GMap.enableContinuousZoom();
			slMap.GMap.enableScrollWheelZoom();

			slMap.GMap.setCenter(new GLatLng(0, 0), 16);

			// Allow user to switch map types
			slMap.GMap.addControl(new GMapTypeControl());

			// Install our various event handlers
			GEvent.addListener( // clicking on the map
				slMap.GMap, 
				"click", 
				function(marker, point){
					SLURL.clickHandler(slMap, marker, point);
				}
			);
			GEvent.addListener( // map stops moving
					slMap.GMap, 
					"moveend", 
					function(){
						slMap.onStateChangedHandler();
					}
			);

			if (SLURL.showHoverTips){ // Enable, If we want mouse move handlers
				GEvent.addListener(
					slMap.GMap,
					"mousemove",
					function(pos){
						slMap.onMouseMoveHandler(pos);
					}
				);
				GEvent.addListener(
					this.GMap,
					"mouseout", 
					function(pos){
						slMap.onMouseOutHandler(pos);
					}
				);
			}
			GEvent.addListener(
				slMap.GMap, 
				"dragstart", 
				function(){
					SLURL.dragHandler(slMap);
				}
			);        

			// Moved this to the end as GMaps seemed to fail if I did it right
			// after map creation, and I don't have time to debug other people's code.
			// --Would be nice to know who wrote the above comment. ~ SignpostMarv
			this.GMarkerManager = new GMarkerManager(this.GMap);
		}else{
			// Browser does not support Google Maps
			this.GMap = null;
			throw 'Your browser is not supported';
		}
	},

	dragHandler                : function(slMap){
		if(slMap.currentMapWindow && slMap.currentMapWindow.options && slMap.currentMapWindow.options.closeOnMove){
			slMap.GMap.closeInfoWindow();
			delete slMap.currentMapWindow;
		}
	},

	clickHandler               : function(slMap, gmarker, point){
		if(!gmarker){
			var slCoord = new SLURL.XYPoint;
			slCoord._SetFromGLatLng(point);
			SLURL.gotoSLURL(slMap, slCoord.x, slCoord.y);
		}else if(gmarker.slMarker){
			var slMarker = gmarker.slMarker;
			if(slMarker.options.centerOnClick){
				slMap.panOrRecenterToSLCoord(slMarker.slCoord);
			}
			if(slMarker.options.clickHandler){
				slMarker.options.clickHandler(slMarker);
			}
		}
	}
}

// Attach SLURL.RegionPoint to the SLURL.XYPoint class
SLURL.RegionPoint.prototype = new SLURL.XYPoint;


// SLURL.EuclideanProjection

// == Attach it to the GProjection() class ==
SLURL.EuclideanProjection.prototype=new GProjection();


// == A method for converting latitudes and longitudes to pixel coordinates == 
SLURL.EuclideanProjection.prototype.fromLatLngToPixel=function(LatLng,zoom)
{
    var RawMapX = LatLng.lng() / SLURL.mapFactor;
    var RawMapY = -LatLng.lat() / SLURL.mapFactor;
    
    // Now map this square onto a 1:1 bitmap of the entire SL map, based
    // on the size of SL map tiles (at zoom level 1, the closest)
    var RawPixelX = RawMapX * SLURL.tileSize;
    var RawPixelY = RawMapY * SLURL.tileSize;
    
    // Now account for the fact that the map may be zoomed out
    zoom = SLURL.convertZoom(zoom);
    var ZoomFactor = Math.pow(2, zoom - 1);

    var PixelX = RawPixelX / ZoomFactor;
    var PixelY = RawPixelY / ZoomFactor;
    
    return new GPoint(PixelX, PixelY)
};

// == a method for converting pixel coordinates to latitudes and longitudes ==

SLURL.EuclideanProjection.prototype.fromPixelToLatLng=function(pos,zoom,c)
{
    // First, account for the fact that the map may be zoomed out
    zoom = SLURL.convertZoom(zoom);
    var ZoomFactor = Math.pow(2, zoom - 1);

    var RawPixelX = pos.x * ZoomFactor;
    var RawPixelY = pos.y * ZoomFactor;
    
    // Now map this 1:1 bitmap position onto a 10k square of SL tiles, located at the origin
    var RawMapX = RawPixelX / SLURL.tileSize;
    var RawMapY = RawPixelY / SLURL.tileSize;
    
    // Now map this 10k SL square onto a 90 LatLng square
    var Lng = RawMapX * SLURL.mapFactor;
    var Lat = RawMapY * SLURL.mapFactor;
    
    return new GLatLng(-Lat,Lng,c)
};
 
// == a method that checks if the x/y value is in range ==
SLURL.EuclideanProjection.prototype.tileCheckRange=function(pos, zoom, tileSize)
{
	return ((pos.x < 0) || (pos.y < 0)) ? false : true;
}

// == a method that returns the width of the tilespace (the bounding box of the map) ==      
SLURL.EuclideanProjection.prototype.getWrapWidth=function(zoom) 
{
	return this.tileBounds[zoom] * SLURL.gridEdgeSizeInRegions;		
}


/////////////////////////////////////////////////////////////////////////////////////////////////
// SL Map API ///////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////


// ------------------------------------
//
//              SLURL.XYPoint
//
//
// ------------------------------------

SLURL.XYPoint.prototype.GetGLatLng = function()
{
    // Invert Y axis
	var corrected_y = SLURL.gridEdgeSizeInRegions - this.y;
    var lat = -corrected_y * SLURL.mapFactor;
    var lng = this.x * SLURL.mapFactor;
	return new GLatLng(lat, lng);
}
 

SLURL.XYPoint.prototype._SetFromGLatLng = function(gpos)
{
    this.x = gpos.lng() / SLURL.mapFactor;
    this.y = -gpos.lat() / SLURL.mapFactor;
    // Invert Y axis back
    this.y = SLURL.gridEdgeSizeInRegions - this.y;
}

// ------------------------------------
//
//               SLURL.Bounds
//
// ------------------------------------

SLURL.Bounds.prototype._SetFromGLatLngBounds = function(gbounds)
{
		var SW = new SLURL.XYPoint();
		var NE = new SLURL.XYPoint();
		
		SW._SetFromGLatLng(gbounds.getSouthWest());
		NE._SetFromGLatLng(gbounds.getNorthEast());
		
		this.xMin = SW.x;
		this.yMin = SW.y;
		
		this.xMax = NE.x;
		this.yMax = NE.y;
}


// ------------------------------------
//
//               Img
//
// ------------------------------------
SLURL.Img.prototype.isAlpha = function(){
	return this.alpha;
}


// ------------------------------------
//
//               Icon
//
// ------------------------------------
SLURL.Icon.prototype.hasShadow = function(){
	return !!this.shadowImg;
};


// ------------------------------------
//
//             MapWindow
//
// ------------------------------------

SLURL.MapWindow.prototype.getGMapOptions = function(){
	return {maxWidth: ((this.options && this.options.width) ? this.options.width : 252)};
}

// ------------------------------------
//
//               SLMap
//
// ------------------------------------


SLURL.Map.prototype.onStateChangedHandler = function(){
	// Service user supplied handler if it exists
	if (this.options && this.options.onStateChangedHandler){
		this.options.onStateChangedHandler();
	}
}

SLURL.Map.prototype.onMouseMoveHandler = function(pos){
	// We just got a mouse move, so the user isn't 'hovering' right now
	this.resetHoverTimeout(true);
	this.hoverPos = pos;
	
	// If we're showing a tooltip, close it
	if (this.showingHoverWindow){
		this.GMap.closeInfoWindow();
	}
}

SLURL.Map.prototype.onMouseOutHandler = function(pos){
	// Mouse is leaving map - clear tooltip timers
	this.clearHoverTimeout(true);
}

SLURL.Map.prototype.clearHoverTimeout = function(){
	if (this.ID != null){
		window.clearTimeout(this.ID);
		this.ID = null;
	}
}

SLURL.Map.prototype.resetHoverTimeout = function(forceTimerSet){
	this.clearHoverTimeout();
	if ((this.ID != null) || forceTimerSet){
		var map = this;
		this.ID = window.setTimeout(function() { map.mousehoverHandler(); }, SLURL.mouseHoverDelay);
	}
}

SLURL.Map.prototype.mousehoverHandler = function(){
	// Get tile coordinate
	var tilePos = new SLURL.XYPoint;
	tilePos._SetFromGLatLng(this.hoverPos);

	var tileX = Math.floor(tilePos.x);
	var tileY = Math.floor(tilePos.y);

	this.showTileToolTip();
}

SLURL.Map.prototype.getRegionName = function(){
	var text = "Test Region Name";
	return text;
}

SLURL.Map.prototype.showTileToolTip = function(){
	var
		map       = this,
		HoverText = this.getRegionName()
	;

	map.ID = null;
	map.GMap.openInfoWindowHtml(map.hoverPos, HoverText, { onCloseFn: function() { map.hoverWindowCloseHandler(); }});
	map.showingHoverWindow = true;
}

SLURL.Map.prototype.hoverWindowCloseHandler = function(){
	// Window has just closed, so reset any hover timer, so a window doesn't appear immediately
	this.showingHoverWindow = false;
	this.resetHoverTimeout(false);	
}

SLURL.Map.prototype.CreateMapTypes = function(){
	var mapTypes = [];
	
		var copyCollection = new GCopyrightCollection('SecondLife');
		var copyright = new GCopyright(1, new GLatLngBounds(new GLatLng(0, 0), new GLatLng(-90, 90)), 0, "(C) 2007 - " + (new Date).getFullYear() + " Linden Lab");
		copyCollection.addCopyright(copyright);

		// Create the 'Land' type of map
		var landTilelayers = [new GTileLayer(copyCollection, 10, 16)];
		landTilelayers[0].getTileUrl = SLURL.getTileUrl;
		
		//var landMap = new GMapType(landTilelayers, this.mapProjection, "Land", {errorMessage:"No SL data available"});
		var landMap = new GMapType(landTilelayers, this.mapProjection, "Land" );
		landMap.getMinimumResolution = function() { return SLURL.convertZoom(SLURL.minZoomLevel); };
		landMap.getMaximumResolution = function() { return SLURL.convertZoom(SLURL.maxZoomLevel); };

		mapTypes.push(landMap);
		
	return mapTypes;
}

SLURL.Map.prototype.CreateMapDiv = function(mainDiv){
	var
		SLMap = this,
		mapDiv = document.createElement("div") // Create a div to be the main map container as a child of the main div
	;
	
	// Match parent height
	mapDiv.style.height = "100%";

	if(SLMap.options.showRegionSearchForm){ // create the div for the text input form
		var
			form          = document.createElement("form"),
			formLabel     = document.createTextNode("Enter region name:"),
			formLabelSpan = document.createElement("span"),
			formText      = document.createElement("input"),
			formButton    = document.createElement("input"),
			clickHandler  = function(){
				if(formText){
					SLURL.getRegionCoordsByName(formText.value, function(pos){
						if(pos.x && pos.y){
							SLMap.panOrRecenterToSLCoord(
								new SLURL.XYPoint(pos.x, pos.y)
							);
						}
					});
					SLMap.gotoRegion(formText.value); 
				}else{
					alert("Can't find textField!");
				}
				return false;
			}
		;
		form.setAttribute('style',[
			'text-align:center',
			'padding:4px',
			'width:270px',
			'margin-left:auto',
			'margin-right:auto',
			'background-color:#fff'
		].join(';'));
		form.onsubmit = clickHandler;

		// Label for the text field
		formLabelSpan.style.fontSize = "80%";
		formLabelSpan.appendChild(formLabel);

		// Text field for the region name
		formText.value = "Ahern";
		formText.size = 15;

		// Button to activate 'go to region'
		formButton.type = "submit";
		formButton.value = "Go!";
		formButton.onsubmit = clickHandler;

		// Put form on the page
		form.appendChild(formLabelSpan);
		form.appendChild(formText);
		form.appendChild(formButton);

		mainDiv.appendChild(form);
	}

	mainDiv.appendChild(mapDiv);

	return mapDiv;
}

SLURL.Map.prototype.gotoRegion = function(regionName){
	var SLMap = this;
	SLURL.getRegionCoordsByName(regionName, function(pos){
		if(pos.x && pos.y){
			SLMap.panOrRecenterToSLCoord(
				new SLURL.XYPoint(pos.x, pos.y)
			);
		}
	});
}

SLURL.Map.prototype.centerAndZoomAtSLCoord = function(pos, zoom){
    if (this.GMap){
        this.GMap.setCenter(pos.GetGLatLng(), SLURL.convertZoom(
			this._forceZoomToLimits(zoom) // Enforce zoom limits specified by client
		));
    }
}

SLURL.Map.prototype.disableDragging = function(){
    if(this.GMap){
        this.GMap.disableDragging();
    }
}

SLURL.Map.prototype.enableDragging = function(){
    if(this.GMap){
		this.GMap.enableDragging();
    }
}

SLURL.Map.prototype.getViewportBounds = function(){
	if (this.GMap){
		var viewBounds = new SLURL.Bounds();
		viewBounds._SetFromGLatLngBounds(this.GMap.getBounds());
		return viewBounds;
	}
}

SLURL.Map.prototype.getMapCenter = function(){
	if(this.GMap){
		var center  = new SLURL.XYPoint();
		center._SetFromGLatLng(this.GMap.getCenter());
		return center;
	}
}

// Simulate a GMap click event on the centre of this marker
SLURL.Map.prototype.clickMarker = function(marker){
	SLURL.clickHandler(this, marker.gmarker, marker.gmarker.getPoint());
}

SLURL.Map.prototype.addMarker = function(marker, mapWindow){
	if (this.GMap){
		var
			markerImg    = marker.icons[0],
			gicon        = new GIcon(),
			width        = markerImg.mainImg.width,
			height       = markerImg.mainImg.height,
			hotspotX     = width / 2,
			hotspotY     = height / 2,
			point        = marker.slCoord.GetGLatLng(),
			isClickable  = (mapWindow || marker.options.centerOnClick || marker.options.clickHandler || marker.options.onMouseOverHandler || marker.options.onMouseOutHandler), // Mouse over/out events are not clicks, but if we're not clickable or draggable, then GMaps doesn't send us any events.
			markerZIndex = (marker.options.zLayer) ? marker.options.zLayer : 0
		;

		gicon.image          = markerImg.mainImg.URL;
		gicon.iconSize       = new GSize(width, height);
		gicon.shadowSize     = gicon.iconSize;
		if(markerImg.shadowImg){
			gicon.shadow     = markerImg.shadowImg.URL;
			gicon.shadowSize = new GSize(markerImg.shadowImg.width, markerImg.shadowImg.height);
		}

		// Work out hotspot of marker
		if(marker.options.horizontalAlign == "left"){
			hotspotX = 0;
		}else if(marker.options.horizontalAlign == "right"){
			hotspotX = gicon.iconSize.width;
		}
		if(marker.options.verticalAlign == "top"){
			hotspotY = 0;
		}else if(marker.options.verticalAlign == "bottom"){
			hotspotY = gicon.iconSize.height;
		}

		gicon.iconAnchor       = new GPoint(hotspotX, hotspotY);
		gicon.infoWindowAnchor = gicon.iconAnchor; // TODO: need to change this? It's probably ok for most cases

		var gmarkeroptions = {
				icon: gicon, 
				clickable: isClickable, 
				draggable: false,
				zIndexProcess: function() { return markerZIndex; }
		};

		// The SL marker 'owns' the GMarker, and we insert a link from GMarker
		// back to SL marker to assist callback/event processing
		marker.gmarker          = new GMarker(point, gmarkeroptions);
		marker.gmarker.slMarker = marker;

		if (mapWindow){
			GEvent.addListener(marker.gmarker, "click", function(){
				marker.gmarker.openInfoWindowHtml(mapWindow.text, mapWindow.getGMapOptions());
				this.currentMapWindow = mapWindow;
			});
		}

		if (marker.options.onMouseOverHandler){
			GEvent.addListener(marker.gmarker, "mouseover",function(){
				marker.options.onMouseOverHandler(marker);
			});
		}

		if (marker.options.onMouseOutHandler){
			GEvent.addListener(marker.gmarker, "mouseout",function(){
				marker.options.onMouseOutHandler(marker);
			});
		}

		this.GMap.addOverlay(marker.gmarker); // Add the GMarker to the map
	}
}

SLURL.Map.prototype.removeMarker = function(marker){
	if (this.GMap && marker.gmarker){
		this.GMap.removeOverlay(marker.gmarker);
		marker.gmarker = null;
	}
}

SLURL.Map.prototype.removeAllMarkers = function(){
	if (this.GMap){
		this.GMap.clearOverlays();
	}
}

SLURL.Map.prototype.addMapWindow = function(mapWindow, pos){
	if (this.GMap){
		this.GMap.openInfoWindowHtml(pos.GetGLatLng(), mapWindow.text, mapWindow.getGMapOptions());
		this.currentMapWindow = mapWindow;
	}
}

SLURL.Map.prototype.zoomIn = function(){
	return this.zoom(this.zoom() - 1);
}

SLURL.Map.prototype.zoomOut = function(){
	return this.zoom(this.zoom() + 1);
}

SLURL.Map.prototype.zoom = function(level){
	if(this.GMap){
		if(level){ // if a level was specified, we need to set the level before returning it
			this.GMap.setZoom(
				SLURL.convertZoom( // zoom needs to be converted first
					this._forceZoomToLimits( // Enforce zoom limits specified by client
						level
					)
				)
			);
		}
		return SLURL.convertZoom(this.GMap.getZoom());
	}
}

SLURL.Map.prototype._forceZoomToLimits = function(zoom){ // Enforce zoom limits specified by client
	if (this.options && this.options.zoomMax){
		zoom = Math.max(zoom, this.options.zoomMax);
	}
	if (this.options && this.options.zoomMin){
		zoom = Math.min(zoom, this.options.zoomMin);
	}	
	return zoom;
}

SLURL.Map.prototype.panBy = function(x, y){
	if (this.GMap){
		var
			pos    = this.GMap.getCenter(),
			offset = this.mapProjection.fromPixelToLatLng(new SLURL.XYPoint(x, y), this.GMap.getZoom()),
			newPos = new GLatLng(pos.lat() + offset.lat(), pos.lng() + offset.lng())
		;
		this.GMap.panTo(newPos);
	}
}

SLURL.Map.prototype.panLeft = function(){
	this.panBy(-SLURL.tileSize, 0);
}

SLURL.Map.prototype.panRight = function(){
	this.panBy(SLURL.tileSize, 0);
}

SLURL.Map.prototype.panUp = function(){
	this.panBy(0, -SLURL.tileSize);
}

SLURL.Map.prototype.panDown = function(){
	this.panBy(0, SLURL.tileSize);
}

SLURL.Map.prototype.panOrRecenterToSLCoord = function(pos, forceCenter){
	if (this.GMap){
		this.GMap.panTo(pos.GetGLatLng());
	}
}
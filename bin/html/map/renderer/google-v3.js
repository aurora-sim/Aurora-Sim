/**
* @license License and Terms of Use
*
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
*/
(function(window, undefined){
	if(!window['mapapi']){
		throw 'mapapi.js not loaded';
	}else if(!window['mapapi']['renderer']){
		throw 'mapapi.js renderer class not loaded';
	}

	var
		document    = window['document'],
		mapapi      = window['mapapi'],
		SLURL       = window['SLURL'],
		google      = window['google']
	;
	if(google == undefined){
		throw 'Google JS not loaded, check JavaScript permissions';
	}
	var
		google_maps = google['maps'],
		GLatLng     = google_maps['LatLng']
	;
	if(google_maps == undefined || GLatLng == undefined){
		throw 'Google Maps API not loaded, check JavaScript permissions';
	}
	var
		renderer    = mapapi['renderer'],
		gridConfig  = mapapi['gridConfig'],
		gridPoint   = mapapi['gridPoint'],
		euclid      = function(gc){
			this.gridConf = gc;
			this.hscale   = 180.0 / this.gridConf['size']['width'];
			this.vscale   = 90.0  / this.gridConf['size']['height'];
		},
		reqAnim    = ['mozRequestAnimationFrame', 'webkitRequestAnimationFrame'],
		reqAnimSp  = false,
		shape      = mapapi['shape'],
		polygon    = shape['polygon'],
		rectangle  = shape['rectangle'],
		square     = shape['square'],
		line       = shape['line'],
		circle     = shape['circle']
	;

	euclid.prototype['fromLatLngToPoint'] = function(latlng, opt){
		var point = opt || new gridPoint(0,0);
		point['x'] = latlng['lng']() / this.hscale;
		point['y'] = latlng['lat']() / this.hscale;
		return point;
	}
	euclid.prototype['fromPointToLatLng'] = function(point){
		return new GLatLng(point['y'] * this.hscale, point['x'] * this.hscale);
	}

	for(var i=0;i<reqAnim.length;++i){
		if(!!window[reqAnim[i]]){
			reqAnim = window[reqAnim[i]];
			reqAnimSp = true;
			break;
		}
	}
	reqAnim = reqAnimSp ? reqAnim : false;

	function google3(options){
		var
			obj      = this,
			options  = options || {},
			gridConf = options['gridConfig']
		;
		if((gridConf instanceof gridConfig) == false){
			throw 'Grid Configuration object must be instance of mapapi.gridConfig';
		}
		obj.gridConfig = gridConf;
		function regionsPerTileEdge(zoom){
			return Math.pow(2, obj.convertZoom(zoom));
		}
		function posZoomToxyZoom(pos, zoom){
			var
				regions_per_tile_edge = regionsPerTileEdge(zoom),
				result = {
					'x' : pos['x'] * regions_per_tile_edge,
					'y' : pos['y'] * regions_per_tile_edge,
					'zoom' : obj.convertZoom(zoom)
				}
			;

			result['x'] -= result['x'] % regions_per_tile_edge;

			result['y'] = -result['y'];
			result['y'] -= regions_per_tile_edge;
			result['y'] -= result['y'] % regions_per_tile_edge;

			return result;
		}

		obj['contentNode'] = document.createElement('div');
		mapapi['utils']['addClass'](obj['contentNode'], 'mapapi-renderer mapapi-renderer-google-v3');
		mapapi['renderer'].call(obj, options);

		if(obj.gridConfig['tileSources'][0]['options']['backgroundColor']){
			options['backgroundColor'] = obj.gridConfig['tileSources'][0]['options']['backgroundColor'];
		}
		options['scrollwheel']        = obj['opts']['scrollWheelZoom'] || !0;
		options['mapTypeControl']     = options['mapTypeControl']         || !1;
		options['overviewMapControl'] = options['overviewMapControl']     || !1;
		options['panControl']         = options['panControl']             || !1;
		options['rotateControl']      = options['rotateControl']          || !1;
		options['scaleControl']       = options['scaleControl']           || !1;
		options['streetViewControl']  = options['streetViewControl']      || !1;
		options['zoomControl']        = options['zoomControl']            || !1;

		options['disableDoubleClickZoom'] = true;

		obj['vendorContent'] = new google_maps['Map'](obj['contentNode'], options);

		obj['options'](options);

		google_maps['event']['addListener'](obj['vendorContent'], 'click', function(e){
			obj['fire']('click', {'pos': obj['GLatLng2gridPoint'](e['latLng'])});
		});
		google_maps['event']['addListener'](obj['vendorContent'], 'bounds_changed', function(){
			obj['fire']('bounds_changed', {'bounds': obj['bounds']()});
		});
		google_maps['event']['addListener'](obj['vendorContent'], 'center_changed', function(){
			var
				pos    = obj['focus'](),
				bounds = obj['bounds']()
			;
			if(bounds == undefined){
				return;
			}
			obj['fire']('focus_changed', {'pos':pos, 'withinBounds' : bounds['isWithin'](pos)});
		});

		obj['scrollWheelZoom'](obj['opts']['scrollWheelZoom']);
		obj['smoothZoom'](obj['opts']['smoothZoom']);

		var
			firstMapType = false,
			mapTypes     = {},
			mapTypeIds   = [],
			size   = this.gridConfig['size'],
			hw     = size['width'] / 2.0,
			hh     = size['height'] / 2.0
		;
		for(var i=0;i<obj.gridConfig['tileSources']['length'];++i){
			var
				tileSource = obj.gridConfig['tileSources'][i],
				label      = tileSource['options']['label']
			;
			mapTypeIds.push(label);
			mapTypes[label] = new google_maps['ImageMapType']({
				'maxZoom'    : tileSource['options']['maxZoom'],
				'minZoom'    : tileSource['options']['minZoom'],
				'tileSize'   : new google_maps['Size'](tileSource['size']['width'], tileSource['size']['height']),
				'isPng'      : (tileSource['options']['mimeType'] == 'image/png'),
				'opacity'    : tileSource['options']['opacity'],
				'getTileUrl' : function(pos,zoom){
					var
						newpos = posZoomToxyZoom(pos,zoom),
						url = tileSource['getTileURL']({'x':newpos['x'], 'y':newpos['y']}, newpos['zoom'])
					;
					return url;
				},
				'alt'        : label,
				'name'       : tileSource['options']['label']
			});
			mapTypes[label]['projection'] = new euclid(gridConf);
			mapTypes[label]['getTileUrl'] = tileSource['getTileURL'];
		}
		for(var i in mapTypes){
			firstMapType = firstMapType ? firstMapType : label;
			obj['vendorContent']['mapTypes']['set'](i,mapTypes[i]);
		}
		obj['vendorContent']['setOptions']({
			'mapTypeIds' : mapTypeIds,
			'mapTypeControlOptions' : {
				'mapTypeIds' : mapTypeIds
			}
		});
		if(firstMapType){
			obj['vendorContent']['setMapTypeId'](firstMapType);
		}

		obj['tileSource'] = gridConf['tileSources'][0];

		obj['dblclickZoom'](obj['opts']['dblclickZoom']);
		if(reqAnim){
			function a(){
				obj['doAnimation']();
				reqAnim(a);
			}
			reqAnim(a);
		}else{
			function b(){
				obj['doAnimation']();
				setTimeout(b, 1000/15);
			}
			b();
		}
	}

	google3.prototype = new renderer;
	google3.prototype['constructor'] = google3;
	google3.prototype['name'] = 'Google Maps v3';
	google3.prototype['description'] = 'Uses version 3 of Google\'s Map API to render the map.';
	google3.prototype['browserSupported'] = true;

	google3.prototype['options'] = function(options){
		renderer.prototype['options']['call'](this, options);
		this['vendorContent']['setOptions'](options);
	}

	google3.prototype.convertZoom = function(zoom){
		return (this.gridConfig['maxZoom'] + 1) - zoom - 1;
	}

	google3.prototype['gridPoint2GLatLng'] = function(pos){
		var
			size   = this.gridConfig['size'],
			hscale = 180.0 / size['height'],
			lat   = (pos['y'] * 2) * hscale,
			lng   = (pos['x'] * 2) * hscale
		;
		return new GLatLng(0 - lat, lng);
	}

	google3.prototype['GLatLng2gridPoint'] = function(pos){
		var
			size   = this.gridConfig['size'],
			hscale = 180.0 / size['height']
		;
		return new gridPoint(
			(pos.lng() / hscale) / 2,
			(pos.lat() / hscale) / -2
		);
	}

	google3.prototype['panTo'] = function(pos, y){
		if(typeof pos == 'number'){
			pos = new gridPoint(pos, y);
		}
		this['vendorContent']['panTo'](this['gridPoint2GLatLng'](pos));
	}

	google3.prototype['zoom'] = function(zoom){
		if(this['vendorContent'] == undefined){
			return 0;
		}
		if(zoom != undefined){
			this['vendorContent']['setZoom'](this.convertZoom(zoom));
			this['fire']('bounds_changed', {'bounds': this['bounds']()});
		}
		return this.convertZoom(this['vendorContent']['getZoom']());
	}
	google3.prototype['focus'] = function(pos, zoom, a){
		if(typeof pos == 'number'){
			pos = new gridPoint(pos, zoom);
			zoom = a;
		}
		if(pos instanceof gridPoint){
			this['vendorContent']['setCenter'](this['gridPoint2GLatLng'](pos));
		}
		if(zoom != undefined){
			this['zoom'](zoom);
		}
		return this['GLatLng2gridPoint'](this['vendorContent']['getCenter']());
	}

	google3.prototype['scrollWheelZoom'] = function(flag){
		var
			obj  = this,
			opts = obj['opts']
		;
		if(flag != undefined){
			flag = !!flag;
			opts['scrollWheelZoom'] = flag;
			obj['vendorContent']['setOptions']({'scrollwheel':flag});
		}
		return obj['vendorContent']['scrollwheel'];
	}

	google3.prototype['draggable'] = function(flag){
		var
			obj  = this,
			opts = obj['opts']
		;
		if(flag != undefined){
			flag = !!flag;
			opts['draggable'] = flag;
			obj['vendorContent']['setOptions']({'draggable':flag});
		}
		return obj['vendorContent']['draggable'];
	}

	google3.prototype['dblclickZoom'] = function(flag){
		var
			obj  = this,
			opts = obj['opts'],
			foo = function(e){
				obj['fire']('dblclick', {
					'pos' : obj.GLatLng2gridPoint(e.latLng)
				});
			}
		;
		renderer.prototype['dblclickZoom'].call(obj, flag);
		if(flag != undefined){
			flag = !!flag;
			if(flag){
				google_maps['event']['addListener'](obj['vendorContent'], 'dblclick', foo);
			}else{
				google_maps['event']['removeListener'](obj['vendorContent'], 'dblclick', foo);
			}
		}
		return opts['dblclickZoom'];
	}

	google3.prototype['bounds'] = function(){
		var
			obj    = this,
			bounds = obj['vendorContent']['getBounds']()
		;
		if(bounds == undefined){
			return undefined;
		}
		return new mapapi['bounds'](obj['GLatLng2gridPoint'](bounds['getSouthWest']()), obj['GLatLng2gridPoint'](bounds['getNorthEast']()));
	}

	function mapapi2google(mapapiShape){
		if(!mapapiShape){
			throw 'Shape not specified';
		}
		if(mapapiShape.prototype instanceof mapapi['shape']){
			
		}
	}

	function color2hex(){
		var
			r = arguments[1] * 1,
			g = arguments[2] * 1,
			b = arguments[3] * 1
		;
		r = (r < 16) ? '0' + r.toString(16) : r.toString(16);
		g = (g < 16) ? '0' + g.toString(16) : g.toString(16);
		b = (b < 16) ? '0' + b.toString(16) : b.toString(16);
		return '#' + r + g + b;
	}

	google3.prototype['addShape'] = function(){
		var
			supported = [square, rectangle, line, polygon, circle],
			isSupported = false,
			mapapiShape,
			path,
			rgbRegex  = /^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$/,
			rgbaRegex = /^rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\,\s*([0|1]|[0|1]?\.\d+)\s*\)$/,
			zoom   = this['zoom'](),
			zoom_a = .5 + (.5 * (1 - (zoom % 1))),
			zoom_b = 1 << zoom
		;
		for(var i=0;i<arguments['length'];++i){
			mapapiShape = arguments[i];
			isSupported = false;
			if(!mapapiShape){
				throw 'Shape not specified';
			}else{
				for(var j=0;j<supported['length'];++j){
					if(mapapiShape instanceof supported[j] || mapapiShape.prototype instanceof supported[j]){
						isSupported = true;
						break;
					}
				}
				if(!isSupported){
					throw 'A shape in the arguments was not supported';
				}
			}
			var
				coords = mapapiShape['coords'](),
				strokeStyle = mapapiShape['strokeStyle'](),
				fillStyle   = mapapiShape['fillStyle'] ? mapapiShape['fillStyle']() : false,
				rgb,
				fillrgb,
				alpha = 0,
				fillalpha = 0
			;
			function setcolors(){
				if(rgbRegex['test'](strokeStyle)){
					rgb = strokeStyle.replace(rgbRegex,color2hex);
				}else if(rgbaRegex['test'](strokeStyle)){
					rgb = strokeStyle.replace(rgbaRegex,color2hex);
					alpha = strokeStyle.replace(rgbaRegex,function(){
						return arguments[4] * 1;
					});
				}
				if(rgbRegex['test'](fillStyle)){
					fillrgb = fillStyle.replace(rgbRegex,color2hex);
				}else if(rgbaRegex['test'](fillStyle)){
					fillrgb = fillStyle.replace(rgbaRegex,color2hex);
					fillalpha = fillStyle.replace(rgbaRegex,function(){
						return arguments[4] * 1;
					});
				}
			}

			if(mapapiShape instanceof line || mapapiShape.prototype instanceof line){
				path = [];
				for(var j=0;j<coords['length'];++j){
					path['push'](this['gridPoint2GLatLng'](coords[j]));
				}
				setcolors();
				mapapiShape['google3'] = new google_maps['Polyline']({
					'path'          : path,
					'strokeColor'   : rgb,
					'strokeOpacity' : alpha,
					'strokeWidth'   : mapapiShape['lineWidth']()
				});
				mapapiShape['google3']['setMap'](this['vendorContent']);
			}else if(mapapiShape instanceof square || mapapiShape.prototype instanceof square || mapapiShape instanceof rectangle || mapapiShape.prototype instanceof rectangle){
				path = [coords[0], new gridPoint(coords[0]['x'], coords[1]['y']), coords[1], new gridPoint(coords[1]['x'], coords[0]['y'])];
				for(var j=0;j<path['length'];++j){
					path[j] = this['gridPoint2GLatLng'](path[j]);
				}
				setcolors();
				mapapiShape['google3'] = new google_maps['Polygon']({
					'path'          : path,
					'strokeColor'   : rgb,
					'strokeOpacity' : alpha,
					'fillColor'     : fillrgb,
					'fillOpacity'   : fillalpha,
					'strokeWidth'   : mapapiShape['lineWidth']()
				});
				mapapiShape['google3']['setMap'](this['vendorContent']);
			}else if(mapapiShape instanceof circle || mapapiShape.prototype instanceof circle){
				setcolors();
				mapapiShape['google3'] = new google_maps['Circle']({
					'center'        : this['gridPoint2GLatLng'](coords[0]),
					'radius'        : mapapiShape['radius']() / (27500.0 / this['gridConfig']['size']['width']),
					'strokeColor'   : rgb,
					'strokeOpacity' : alpha,
					'fillColor'     : fillrgb,
					'fillOpacity'   : fillalpha,
					'strokeWidth'   : mapapiShape['lineWidth']()
				});
				mapapiShape['google3']['setMap'](this['vendorContent']);
			}else if(mapapiShape instanceof polygon || mapapiShape.prototype instanceof polygon){
				path = [];
				for(var j=0;j<coords['length'];++j){
					path['push'](this['gridPoint2GLatLng'](coords[j]));
				}
				setcolors();
				mapapiShape['google3'] = new google_maps['Polygon']({
					'path'          : path,
					'strokeColor'   : rgb,
					'strokeOpacity' : alpha,
					'fillColor'     : fillrgb,
					'fillOpacity'   : fillalpha,
					'strokeWidth'   : mapapiShape['lineWidth']()
				});
				mapapiShape['google3']['setMap'](this['vendorContent']);
			}
		}
		renderer.prototype['addShape']['apply'](this, arguments);
	}

	google3.prototype['removeShape'] = function(){
		for(var i=0;i<arguments['length'];++i){
			if(arguments[i]['google3'] != undefined){
				arguments[i]['google3']['setMap'](null);
			}
		}
		renderer.prototype['removeShape']['apply'](this, arguments);
	}

	mapapi['renderers']['google3'] = google3;
})(window);
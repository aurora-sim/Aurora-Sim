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
		document   = window['document'],
		mapapi     = window['mapapi'],
		SLURL      = window['SLURL'],
		renderer   = mapapi['renderer'],
		gridConfig = mapapi['gridConfig'],
		gridPoint  = mapapi['gridPoint'],
		bounds     = mapapi['bounds'],
		size       = mapapi['size'],
		reqAnim    = ['mozRequestAnimationFrame', 'webkitRequestAnimationFrame'],
		reqAnimSp  = false,
		shape      = mapapi['shape'],
		poly       = shape != undefined ? shape['polygon']   : undefined,
		rectangle  = shape != undefined ? shape['rectangle'] : undefined,
		line       = shape != undefined ? shape['line']      : undefined,
		circle     = shape != undefined ? shape['circle']    : undefined
	;

	for(var i=0;i<reqAnim.length;++i){
		if(!!window[reqAnim[i]]){
			reqAnim = window[reqAnim[i]];
			reqAnimSp = true;
			break;
		}
	}
	reqAnim = reqAnimSp ? reqAnim : false;

	function canvas(options){
		renderer['call'](this, options);
		var supported = document.createElement('canvas');
		if(supported){
			supported = (supported['getContext'] && supported['getContext']('2d'));
		}
		if(!supported){
			throw 'Browser does not support canvas renderer';
		}
		var
			obj        = this,
			options    = options || {},
			gridConf = options['gridConfig'],
			clickpan = function(e){
				if(obj['dragging'] == false){
					clearTimeout(obj.mousedown_timer);
					var
						x     = e['clientX'],
						y     = e['clientY'],
						point = obj['px2point'](x - this['offsetLeft'], y - this['offsetTop'])
					;
					obj['panTo'](point);
				}
			}
		;
		if((gridConf instanceof gridConfig) == false){
			throw 'Grid Configuration object must be instance of mapapi.gridConfig';
		}
		obj.gridConfig = gridConf;

		obj['contentNode']   = document.createElement('canvas');
		obj['vendorContent'] = obj['contentNode']['getContext']('2d');

		mapapi['utils']['addClass'](obj['contentNode'], 'mapapi-renderer mapapi-renderer-canvas');
		mapapi['renderer'].call(obj);

		options['minZoom'] = Math.min(gridConf['maxZoom'], options['minZoom'] || 0);
		options['maxZoom'] = Math.max(gridConf['maxZoom'], options['maxZoom'] || 0);

		options['zoom']  = options['zoom'] || 0;
		options['focus'] = options['focus'] || new gridPoint(0,0);

		obj.tileSource = gridConf['tileSources'][0];

		obj['options'](options);

		obj.grid_images = {};

		obj.dirty = true;
		obj.draw(obj['opts']['fps']);
	};

	canvas.prototype = new renderer;
	canvas.prototype['constructor'] = canvas;
	canvas.prototype['name'] = '2D Canvas';
	canvas.prototype['description'] = 'Uses the 2D canvas API to render the map as a single image.';

	var
		canvasElement = document['createElement']('canvas')
	;
	canvas.prototype['browserSupported'] = canvasElement && canvasElement['getContext'] && canvasElement['getContext']('2d');

	canvas.prototype['options'] = function(options){
		renderer.prototype['options']['call'](this, options);
		var
			hasFunc = ['fps'],
			checkFunc
		;
		for(var i=0;i<hasFunc.length;++i){
			var
				checkFunc = hasFunc[i]
			;
			if(options[checkFunc] != undefined){
				obj[checkFunc](options[checkFunc]);
			}
		}
	}

	canvas.prototype['fps'] = function(value){
		if(typeof value == 'number'){
			var
				opts = this['opts']
			;
			opts['fps'] = Math.max(1, value);
		}
		return opts['fps'];
	}

	canvas.prototype.imageQueued = function(x, y, zoom){
		var
			obj = this,
			zi = Math.floor(zoom)
			zoom_b = 1 << zi,
			images = obj.grid_images,
			y = y - (y % zoom_b),
			x = x - (x % zoom_b)
		;
		return (images[zi] && images[zi][x] && images[zi][x][y] instanceof Image);
	}

	canvas.prototype.getImage = function(x, y, zoom, preload){
		var
			obj     = this,
			zi      = Math.floor(zoom),
			zoom_b  = 1 << zi,
			images  = obj.grid_images,
			y       = y - (y % zoom_b),
			x       = x - (x % zoom_b),
			preload = !!preload
		;
		if(preload){
			var px, py, pzi;

			pzi = zi + 1;
			px = x - (x % pzi);
			py = y - (y % pzi);
			if(zi < obj['maxZoom']() && !obj.imageQueued(px, py, pzi)){
				obj.getImage(px, py, pzi);
			}
		}
		if(!images[zi]){
			images[zi] = [];
		}
		if(!images[zi][x]){
			images[zi][x] = [];
		}
		if(!images[zi][x][y]){
			images[zi][x][y] = new Image;
			images[zi][x][y]['_mapapi'] = {
				'x' : x,
				'y' : y,
				'preloaded' : (preload == true)
			};
			images[zi][x][y]['onload'] = function(){
				this['_mapapi']['loaded'] = true;
				if(obj.bounds()['isWithin'](this['_mapapi']['x'], this['_mapapi']['y'])){
					obj.dirty = true;
				}
			}
			images[zi][x][y]['src'] = obj.tileSource['getTileURL'](new gridPoint(x, y), zi);
		}
		return images[zi][x][y];
	}

	canvas.prototype.draw = function(fps){
		fps = Math.max(1, fps || 0);
		var
			obj     = this,
			cbounds = obj['bounds']()
		;
		if(obj.lastsize == undefined || (obj.lastsize['width'] != obj['contentNode']['clientWidth'] || obj.lastsize['height'] != obj['contentNode']['clientHeight'])){
			obj.lastbounds = undefined;
		}
		obj.dirty = obj.dirty || obj['doAnimation']() || (obj.lastbounds == undefined || !obj.lastbounds['equals'](cbounds)) ;
		if((obj.lastbounds == undefined || !obj.lastbounds['equals'](cbounds))){
			obj['fire']('bounds_changed', {'bounds':cbounds});
		}
		obj.lastbounds = cbounds;
		obj.lastsize   = new mapapi['size'](obj['contentNode']['clientWidth'], obj['contentNode']['clientHeight']);
		if(obj.dirty){
			var
				ctx     = obj['vendorContent'],
				canvas  = ctx.canvas
			;
			canvas.width = canvas.clientWidth;
			canvas.height = canvas.clientHeight;
			ctx.save();

			var
				zoom    = obj['zoom'](),
				zoom_a  = .5 + (.5 * (1 - (zoom % 1))),
				zoom_b  = 1 << Math.floor(zoom),
				zoom_c  = 1 << Math.floor(zoom - 1),
				focus   = obj['focus'](),
				cWidth  = canvas['width'],
				cWidth2 = cWidth / 2.0,
				cHeight = canvas['height'],
				cHeight2= cHeight / 2.0,
				size    = obj.tileSize(),
				tWidth  = size['width'],
				tHeight = size['height'],
				images  = [],
				startX  = cbounds['sw']['x'] - (cbounds['sw']['x'] % zoom_b),
				startY  = cbounds['sw']['y'] - (cbounds['sw']['y'] % zoom_b),
				sbounds = new bounds(new gridPoint(startX, startY), new gridPoint(cbounds['ne']['x'], cbounds['ne']['y'])) 
			;
			ctx.fillStyle = obj['tileSource']['options']['backgroundColor'];
			ctx.fillRect(0,0, cWidth, cHeight);

			ctx.translate((focus['x'] * -tWidth) + cWidth2,(focus['y'] * tHeight) + cHeight2 - (tHeight * zoom_b));
			ctx.scale(tWidth, tHeight);

			for(var x = startX; x<=cbounds['ne']['x']; x += zoom_b){
				for(var y = startY; y<=cbounds['ne']['y']; y += zoom_b){
					var img = obj.getImage(x, y, zoom);
					if(img['_mapapi'].loaded){
						ctx.drawImage(
							img,
							img['_mapapi'].x,
							-img['_mapapi'].y,
							zoom_b, zoom_b);
					}
				}
			}

			if(shape != undefined){
				var
					shapes = obj['shapes']()['intersects'](sbounds),
					currentShape,lineWidth
				;
				for(var i=0;i<shapes['length'];++i){
					lineWidth = false;
					currentShape = shapes[i];
					if(currentShape instanceof shape){
						if(currentShape['fillStyle'] != undefined){
							ctx['fillStyle'] = currentShape['fillStyle']();
						}
						lineWidth = currentShape['lineWidth']();
						if(lineWidth > 0){
							ctx['strokeStyle'] = currentShape['strokeStyle']();
							ctx['lineWidth'] = lineWidth = (lineWidth / tWidth) / zoom_b;
						}else{
							lineWidth = false;
						}
						if(currentShape instanceof rectangle){
							var
								rectX = currentShape['bounds']['sw']['x'],
								rectY = currentShape['bounds']['ne']['y'],
								rectW = currentShape['bounds']['ne']['x'] - rectX,
								rectH = rectY - currentShape['bounds']['sw']['y'],
								rectY = -rectY + zoom_b;
							;
							ctx['fillRect'](rectX, rectY, rectW, rectH);
							if(lineWidth > 0){
								ctx['strokeRect'](rectX, rectY, rectW, rectH);
							}
						}else if(currentShape instanceof poly){
							var coords = currentShape['coords']();
							if(coords['length'] >= 3){
								ctx['beginPath']();
								ctx['moveTo'](coords[0]['x'], -coords[0]['y'] + zoom_b);
								for(var j=1;j<coords['length'];++j){
									ctx['lineTo'](coords[j]['x'], -coords[j]['y'] + zoom_b);
								}
								ctx['closePath']();
								ctx['fill']();
								if(lineWidth > 0){
									ctx['stroke']();
								}
							}
						}else if(currentShape instanceof line && lineWidth > 0){
							var
								coords = currentShape['coords']()
							;
							if(coords['length'] >= 2){
								ctx['beginPath']();
								ctx['moveTo'](coords[0]['x'], -coords[0]['y'] + zoom_b);
								for(var j=1;j<coords['length'];++j){
									ctx['lineTo'](coords[j]['x'], -coords[j]['y'] + zoom_b);
								}
								ctx['stroke']();
							}
						}else if(currentShape instanceof circle){
							var
								currentShapePos = currentShape['coords']()[0],
								currentShapeRadius = currentShape['radius']()
							;
							if(currentShapePos instanceof gridPoint && currentShapeRadius > 0){
								ctx['beginPath']();
								ctx['arc'](currentShapePos['x'], -currentShapePos['y'] + zoom_b, currentShapeRadius, 0, Math.PI * 2, true);
								ctx['closePath']();
								ctx['fill']();
								if(lineWidth > 0){
									ctx['stroke']();
								}
							}
						}
					}
				}
			}
			
			ctx.restore();

			obj.dirty = false;
		}
		if(reqAnimSp){
			reqAnim(function(){ obj.draw() });
		}else{
			setTimeout(function(){ obj.draw(fps) },1000/fps);
		}
	}

	canvas.prototype['focus'] = function(pos, zoom, a){
		if(typeof pos == 'number'){
			pos = new gridPoint(pos, zoom),
			zoom = a;
		}
		if(zoom == undefined){
			zoom = this['zoom']();
		}
		var obj = this;
		if(pos){
			renderer.prototype['focus'].call(obj, pos, zoom);
		}
		return renderer.prototype['focus'].call(obj);
	}

	canvas.prototype['panTo'] = function(pos, y){
		if(typeof pos == 'number'){
			pos = new gridPoint(pos, y);
		}
		var obj = this;
		this['animate']({
			'focus' : pos
		}, .5);
	}

	canvas.prototype['scrollWheelZoom'] = function(flag){
		var
			obj        = this,
			opts       = obj['opts'],
			zoomStuffs = function(e){
				var d=0;
				if(!e){
					e = window['event']
				}else if(e['wheelDelta']){
					d = e['wheelDelta'] / 120;
					if(window['opera']){
						d = -d;
					}
				}else if(e['detail']){
					d = -e['detail'] / 3;
				}
				if(d){
					var
						zoom = obj['zoom'](),
						mod  = (d > 0) ? -1 : 1
					;
					if(obj['smoothZoom']()){
						obj['animate']({
							'zoom' : (zoom + mod)
						}, .5);
					}else{
						obj['zoom'](zoom + mod);
					}
				}
				if(e['preventDefault']){
					e['preventDefault']();
				}
				e['returnValue'] = false;
				return false;
			}
		;
		if(flag != undefined){
			flag = !!flag;
			opts['scrollWheelZoom'] = flag;
			if(flag){
				if(window['addEventListener']){
					obj['contentNode']['addEventListener'](/WebKit/.test(window['navigator']['userAgent']) ? 'mousewheel' : 'DOMMouseScroll', zoomStuffs, false);
				}else if(window['attachEvent']){
					obj['contentNode']['attachEvent']('onmousewheel', zoomStuffs);
				}
			}else{
				if(window['removeEventListener']){
					obj['contentNode']['removeEventListener']('DOMMouseScroll', zoomStuffs, false);
				}else if(window['detachEvent']){
					obj['contentNode']['detachEvent']('onmousewheel', zoomStuffs);
				}
			}
		}
		return opts['scrollWheelZoom'];
	}

	canvas.prototype['draggable'] = function(flag){
		var
			obj  = this,
			opts = obj['opts'],
			dragstart_pos     = undefined,
			mousedown_handler = function(e){
				var
					x = e['clientX'],
					y = e['clientY']
				;
				dragstart_pos = obj['px2point'](x - this['offsetLeft'], y - this['offsetTop']);
				clearTimeout(obj.mousedown_timer);
				obj['dragging'] = false;
				obj.mousedown_timer = setTimeout(function(){
					obj['dragging'] = true;
				}, 100);
			},
			mouseup_handler   = function(e){
				clearTimeout(obj.mousedown_timer);
				if(!obj['dragging']){
					obj['fire']('click',{'pos':obj['px2point'](e['offsetX'] != undefined ? e['offsetX'] : e['pageX'] - e['target']['offsetLeft'], e['offsetY'] != undefined ? e['offsetY'] : e['pageY'] - e['target']['offsetTop'])});
				}
				obj.mousedown_timer = setTimeout(function(){
					obj['dragging'] = false;
				}, 100);
			},
			mousemove_handler = function(e){
				if(obj['dragging']){
					var
						x     = e['clientX'],
						y     = e['clientY'],
						point = obj['px2point'](x - this['offsetLeft'], y - this['offsetTop']),
						focus = obj['focus']()
					;
					obj['fire']('drag',{
						'to': new gridPoint(
							focus['x'] - (point['x'] - dragstart_pos['x']),
							focus['y'] - (point['y'] - dragstart_pos['y'])
						)
					});
				}
			}
		;
		if(flag != undefined){
			flag = !!flag;
			opts['draggable'] = flag;
			if(flag){
				obj['contentNode']['addEventListener']('mousedown', mousedown_handler, false);
				obj['contentNode']['addEventListener']('mouseup'  , mouseup_handler  , false);
				obj['contentNode']['addEventListener']('mousemove', mousemove_handler, false);
			}else{
				obj['contentNode']['removeEventListener']('mousedown', mousedown_handler, false);
				obj['contentNode']['removeEventListener']('mouseup'  , mouseup_handler  , false);
				obj['contentNode']['removeEventListener']('mousemove', mousemove_handler, false);
			}
		}
		return opts['draggable'];
	}

	canvas.prototype['dblclickZoom'] = function(flag){
		var
			obj  = this,
			opts = obj['opts'],
			dblclickzoom = function(e){
				var
					x     = e['clientX'],
					y     = e['clientY'],
					point = obj['px2point'](x - this['offsetLeft'], y - this['offsetTop'])
				;
				obj['fire']('dblclick', {
					'pos' : point
				});
			}
		;
		renderer.prototype['dblclickZoom'].call(obj, flag);
		if(flag != undefined){
			flag = !!flag;
			if(flag){
				obj['contentNode']['addEventListener']('dblclick', dblclickzoom, false);
			}else{
				obj['contentNode']['removeEventListener']('dblclick', dblclickzoom, false);
			}
		}
		return opts['dblclickZoom'];
	}

	canvas.prototype['addShape'] = function(){
		var
			ret = renderer.prototype['addShape']['apply'](this, arguments)
		;
		this['dirty'] = this['dirty'] ? !0 : ret;
		return ret;
	}

	canvas.prototype['removeShape'] = function(){
		var
			ret = renderer.prototype['removeShape']['apply'](this, arguments)
		;
		this['dirty'] = this['dirty'] ? !0 : ret;
		return ret;
	}

	mapapi['renderers']['canvas'] = canvas;
})(window);
/**
* License and Terms of Use
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
	window['mapapi'] = window['mapapi'] || {};
	var
		document    = window['document'],
		EventTarget = window['EventTarget'],
		mapapi      = window['mapapi'],
		gridPoint   = mapapi['gridPoint'],
		bounds      = mapapi['bounds'],
		size        = mapapi['size'],
		empty       = mapapi['utils']['empty'],
		shape       = mapapi['shape']
	;
	if(EventTarget == undefined){
		throw 'EventTarget not loaded';
	}

	function each(array, cb){
		for(var i=0;i<array.length;++i){
			cb(array[i],i);
		}
	}

	function dblclick_handler(e){
		var
			obj   = this,
			point = e['pos'],
			zoom  = obj['zoom']() - 1
		;
		if(this['smoothZoom']()){
			this['animate']({
				'zoom'  : zoom,
				'focus' : point
			}, .5);
		}else{
			obj['zoom'](zoom);
			obj['focus'](point);
		}
	}
	function dragpan(e){
		var
			obj = this,
			pos = e['to']
		;
		obj['focus'](pos);
	}

/**
*	@constructor
*/
	function renderer(){
		var
			obj        = this
		;
		EventTarget['call'](this);

		obj['opts'] = {'shapes':new mapapi['shapeManager']};
		obj['_focus'] = new gridPoint(0,0);
	}

	renderer.prototype = new EventTarget();
	renderer.prototype['constructor'] = renderer;
	renderer.prototype['browserSupported'] = false;

	renderer.prototype['options'] = function(options){
		var
			obj       = this,
			options   = options || {},
			opts      = obj['opts'],
			container = options['container'],
			hasFunc   = ['minZoom', 'maxZoom', 'scrollWheelZoom', 'smoothZoom', 'draggable', 'dblclickZoom', 'zoom', 'focus', 'panUnitLR', 'panUnitUD'],
			checkFunc
		;

		options['minZoom']   = options['minZoom']   || 0;
		options['maxZoom']   = options['maxZoom']   || 0;
		options['panUnitLR'] = options['panUnitLR'] || 0;
		options['panUnitUD'] = options['panUnitUD'] || 0;

		for(var i=0;i<hasFunc.length;++i){
			var
				checkFunc = hasFunc[i]
			;
			if(options[checkFunc] != undefined){
				obj[checkFunc](options[checkFunc]);
			}
		}

		obj['addListener']('drag', dragpan);
		obj['addListener']('click', function(e){
			opts['shapes']['click'](e['pos']);
		});

		if(container){
			if(!container['appendChild']){
				throw 'Container is invalid';
			}else{
				opts['container'] = container;
				if(obj['contentNode']){
					obj['contentNode']['style']['width']  = '100%';
					obj['contentNode']['style']['height'] = '100%';
					empty(container)['appendChild'](obj['contentNode']);
				}
			}
		}
	}

	renderer.prototype['minZoom'] = function(value){
		var
			opts = this['opts']
		;
		if(value != undefined){
			opts['minZoom'] = Math.max(0, value);
		}
		return opts['minZoom'];
	};

	renderer.prototype['maxZoom'] = function(value){
		var
			opts = this['opts']
		;
		if(value != undefined){
			opts['maxZoom'] = Math.max(this.minZoom() + 1, value);
		}
		return opts['maxZoom'];
	}

	renderer.prototype['zoom'] = function(value){
		var
			obj  = this,
			opts = obj['opts']
		;
		if(value != undefined){
			opts['zoom'] = Math.min(Math.max(value, obj['minZoom']()), obj['maxZoom']());
		}
		return opts['zoom'];
	}

	renderer.prototype['panUnitUD'] = function(value){
		var
			opts = this['opts']
		;
		if(value){
			opts['panUnitUD'] = Math.max(value, 1);
		}
		return opts['panUnitUD'] * Math.pow(2, this['zoom']());
	}

	renderer.prototype['panUnitLR'] = function(value){
		var
			opts = this['opts']
		;
		if(value){
			opts['panUnitLR'] = Math.max(value, 1);
		}
		return opts['panUnitLR'] * Math.pow(2, this['zoom']());
	}

	renderer.prototype['panTo'] = function(pos, a){
		if(typeof pos == 'number' && typeof a == 'number'){
			pos = new gridPoint(pos, a);
		}else if(typeof pos == 'object' && typeof pos['x'] == 'number' && typeof pos['y'] == 'number'){
			pos = new gridPoint(pos['x'], pos['y']);
		}
		if(pos instanceof gridPoint){
			if(this['bounds']()['isWithin'](pos)){
				this['animate']({
					'focus' : pos
				}, .5);
			}else{
				this['focus'](pos);
			}
		}
	}

	renderer.prototype['panUp'] = function(){
		var pos = this.focus();
		this.panTo(pos['x'], pos['y'] + this.panUnitUD());
	}

	renderer.prototype['panDown'] = function(){
		var pos = this.focus();
		this.panTo(pos['x'], pos['y'] - this.panUnitUD());
	}

	renderer.prototype['panLeft'] = function(){
		var pos = this.focus();
		this.panTo(pos['x'] - this.panUnitLR(), pos['y']);
	}

	renderer.prototype['panRight'] = function(){
		var pos = this.focus();
		this.panTo(pos['x'] + this.panUnitLR(), pos['y']);
	}

	renderer.prototype['scrollWheelZoom'] = function(flag){
		var
			opts = this['opts']
		;
		if(flag != undefined){
			opts['scrollWheelZoom'] = !!flag;
		}
		return opts['scrollWheelZoom'];
	}

	renderer.prototype['smoothZoom'] = function(flag){
		var
			obj  = this,
			opts = obj['opts']
		;
		if(flag != undefined){
			opts['smoothZoom'] = !!flag;
		}
		return opts['smoothZoom'];
	}

	renderer.prototype['draggable'] = function(flag){
		if(flag != undefined){
			if(flag){ // do stuff to make the map renderer draggable
				return true;
			}else{ // do stuff to make it non-draggable
				return false;
			}
		}
		return flag; // should return from other property
	}

	renderer.prototype['focus'] = function(pos, zoom, a){ // should return an instance of mapapi.gridPoint
		if(typeof pos == 'number'){
			pos  = new mapapi['gridPoint'](pos, zoom);
			zoom = this['zoom']();
		}
		if(zoom != undefined){
			this['zoom'](zoom);
		}
		var
			opts = this['opts']
		;
		if(pos instanceof mapapi['gridPoint']){ // implementations should do something to update the renderer to the focal point
			opts['focus'] = pos;
			this['fire']('focus_changed', {'pos':pos, 'withinBounds' : this['bounds']()['isWithin'](pos)});
		}
		return opts['focus'];
	}

	renderer.prototype['px2point'] = function(x, y){
		var
			obj     = this,
			content = obj['contentNode'],
			cWidth  = content['width'],
			cw2     = cWidth / 2.0,
			cHeight = content['height'],
			ch2     = cHeight / 2.0,
			size    = obj['tileSize'](),
			distX   = (x - cw2) / size['width'],
			distY   = ((cHeight - y) - ch2) / size['height'],
			focus   = obj['focus']()//,
			mapX    = focus['x'] + distX,
			mapY    = focus['y'] + distY
		;
		return new gridPoint(mapX, mapY);
	}

	renderer.prototype['point2px'] = function(x, y){
		if(x instanceof gridPoint){
			y = x['y'];
			x = x['x'];
		}
		var
			content = this['contentNode'],
			cWidth  = content['clientWidth'],
			cw2     = cWidth / 2.0,
			cHeight = content['clientHeight'],
			ch2     = cHeight / 2.0,
			size    = this['tileSize'](),
			focus   = this['focus'](),
			fx      = focus['x'],
			fy      = focus['y'],
			diffX   = (x - fx) * size['width'],
			diffY   = (y - fy) * size['height']
		;
		return {'x':cw2 + diffX, 'y':ch2 - diffY};
	}

	renderer.prototype['dblclickZoom'] = function(flag){
		var
			obj  = this,
			opts = obj['opts']
		;
		if(flag != undefined){
			opts['dblclickZoom'] = !!flag;
			if(obj['contentNode']){
				if(!!flag){
					obj['addListener']('dblclick', dblclick_handler);
				}else{
					obj['removeListener']('dblclick', dblclick_handler);
				}
			}
		}
		return opts['dblclickZoom']; // should return from other property
	}

	renderer.prototype['animate'] = function(opts, time){
		if(opts == undefined || (opts != undefined && typeof time != 'number')){
			return;
		}
		var
			obj       = this,
			time      = (!time || time < 0) ? 1 : time,
			czoom     = obj['zoom'](),
			mnzm      = obj['minZoom'](),
			mxzm      = obj['maxZoom'](),
			cpos      = obj['focus'](),
			gridPoint = mapapi['gridPoint'],
			zoom,
			pos,
			animateOrder
		;
		if(typeof opts == 'number'){
			opts = {'zoom':opts};
		}else if(opts instanceof gridPoint || (typeof opts == 'object' && typeof opts['x'] == 'number' && typeof opts['y'] == 'number')){
			opts = {'focus':(opts instanceof gridPoint) ? opts : new gridPoint(opts['x'],opts['y'])};
		}
		pos = animateOrder = !1;
		if(opts['zoom'] != undefined){
			zoom = (typeof opts['zoom'] == 'number') ? opts['zoom'] : (opts['zoom'] * 1);
			zoom = (zoom != czoom && zoom >= mnzm && zoom <= mxzm) ? zoom : undefined;
		}
		if(opts['focus'] instanceof gridPoint){
			pos  = (opts['focus']['x'] != cpos['x'] || opts['focus']['y'] != cpos['y']) ? opts['focus'] : !1;
		}
		var
			a = (zoom != undefined),
			b = !!pos
		;
		if(a || b){
			animateOrder = {};
			if(zoom != undefined){
				animateOrder['zoom']      = zoom;
				animateOrder['fromZoom']  = czoom;
			}
			if(b){
				animateOrder['focus']     = pos;
				animateOrder['fromFocus'] = cpos;
			}
			animateOrder['start'] = (new Date().getTime()) / 1000;
			animateOrder['end']   = animateOrder['start'] + time;
			obj['animateOrder']   = animateOrder;
		}
	}
	renderer.prototype['doAnimation'] = function(){
		var
			obj = this,
			ao  = obj['animateOrder']
		;
		if(!ao){
			return false;
		}
		var
			a   = ao['zoom'],
			b   = ao['fromZoom'],
			c   = ao['focus'],
			d   = ao['fromFocus'],
			e   = ao['start'],
			f   = ao['end'],
			now,diff,g,h,i
		;
		if(!!ao){
			now  = (new Date().getTime()) / 1000;
			diff = (now - e) / (f - e);
			if(now >= ao['end']){
				if(!!c){
					obj['focus'](c, !!a ? a : undefined);
				}else if(!!a){
					obj['zoom'](a);					
				}
				obj['animateOrder'] = !1;
				return true;
			}else if(now > ao['start']){
				i = (a != undefined) ? (b + ((a - b) * diff)) : undefined;
				if(!!c){
					g = !!c ? (d['x'] + ((c['x'] - d['x']) * diff)) : 0;
					h = !!c ? (d['y'] + ((c['y'] - d['y']) * diff)) : 0;
					obj['focus'](g, h, i);
				}else if(i != undefined){
					obj['zoom'](i);
				}
				return true;
			}
		}
		return false;
	}

	renderer.prototype['bounds'] = function(){
		var
			obj     = this,
			content = obj['contentNode'],
			zoom    = obj['zoom'](),
			zoom_a  = .5 + (.5 * (1 - (zoom % 1))),
			zoom_b  = 1 << Math.floor(zoom),
			focus   = obj['focus'](),
			cWidth  = content['width'],
			cHeight = content['height'],
			tWidth  = (obj.tileSource['size']['width'] * zoom_a) / zoom_b,
			tHeight = (obj.tileSource['size']['height'] * zoom_a) / zoom_b,
			wView   = Math.ceil(cWidth / tWidth),
			hView   = Math.ceil(cHeight / tHeight),
			wVhalf  = Math.ceil(wView / 2.0),
			hVhalf  = Math.ceil(hView / 2.0)
		;
		return new bounds({'x': focus['x'] - wVhalf, 'y': focus['y'] - hVhalf},{'x': focus['x'] + wVhalf,  'y': focus['y'] + hVhalf});
	}

	renderer.prototype['tileSize'] = function(){
		var
			obj = this,
			zoom    = obj['zoom'](),
			zoom_a  = .5 + (.5 * (1 - (zoom % 1))),
			zoom_b  = 1 << Math.floor(zoom),
			tWidth  = (obj['tileSource']['size']['width'] * zoom_a) / zoom_b,
			tHeight = (obj['tileSource']['size']['height'] * zoom_a) / zoom_b
		;
		return new size(tWidth, tHeight);
	}

	renderer.prototype['addShape'] = function(){ // bool return indicates whether shape was added
		var
			ret = false
		;
		if(shape != undefined){
			for(var i=0;i<arguments.length;++i){
				if(arguments[i] instanceof shape && this['opts']['shapes']['indexOf'](arguments[i]) < 0){
					ret = true;
					this['opts']['shapes']['push'](arguments[i]);
				}
			}
		}
		return ret;
	}

	renderer.prototype['removeShape'] = function(){
		var
			ret = false
		;
		if(shape != undefined){
			for(var i=0;i<arguments.length;++i){
				if(arguments[i] instanceof shape){
					var
						pos = this['opts']['shapes']['indexOf'](arguments[i])
					;
					if(pos >= 0){
						ret = true;
						this['opts']['shapes']['splice'](pos,1);
					}
				}
			}
		}
		return ret;
	}

	renderer.prototype['shapes'] = function(){
		return this['opts']['shapes'];
	}

	mapapi['renderer']  = renderer;
	mapapi['renderers'] = {};
})(window);
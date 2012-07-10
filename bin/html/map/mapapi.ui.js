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
		document      = window['document'],
		EventTarget   = window['EventTarget'],
		Image         = window['Image'],
		Array         = window['Array'],
		createElement = function(a){ return document['createElement'](a); },
		createText    = function(a){ return document['createTextNode'](a); },
		mapapi        = window['mapapi'],
		gridPoint     = mapapi['gridPoint'],
		bounds        = mapapi['bounds'],
		utils         = mapapi['utils'],
		addClass      = utils ? utils['addClass']    : undefined,
		delClass      = utils ? utils['delClass']    : undefined,
		hasClass      = utils ? utils['hasClass']    : undefined,
		toggleClass   = utils ? utils['toggleClass'] : undefined,
		empty         = utils ? utils['empty']       : undefined,
		ctype_digit   = utils ? utils['ctype_digit'] : undefined,
		trimRegex     = /^\s+|\s+$/g
	;

	function extend(a,b){
		a.prototype = new b;
		a.prototype['constructor'] = a;
	}

	var
		uiID = 0
	;
	function ui(options){
		if(options == undefined){
			return;
		}
		var
			obj               = this,
			options           = options || {},
			container         = options['container'],
			renderer          = options['renderer'],
			gridConfig        = options['gridConfig'],
			markerMngr        = options['markerManager'],
			rendererNode      = createElement('div'),
			sidebarsContainer = createElement('ul')
		;
		if(markerMngr == undefined){
			markerMngr = new markerManager;
		}else if(!(markerMngr instanceof markerManager)){
			throw 'marker manager must be an instance of mapapi.markerManager';
		}
		if(container == undefined){
			container = document['body'];
		}
		if(mapapi['renderer'] == undefined){
			throw 'mapapi.js core not loaded';
		}else if(mapapi['gridConfig'] == undefined){
			throw 'mapapi.gridConfig not loaded';
		}else if(gridConfig == undefined){
			throw 'no grid config specified';
		}else if(!(gridConfig instanceof mapapi['gridConfig'])){
			throw 'grid config is not an instance of mapapi.gridConfig';
		}else if(renderer == undefined){
			var
				canvas = createElement('canvas')
			;
			try{
				if(canvas['getContext'] && !!canvas['getContext']('2d')){
					renderer = mapapi['renderers']['canvas'];
				}
			}catch(e){}
			if(renderer == undefined){
				renderer = mapapi['renderers']['google3'];
			}
			if(renderer == undefined){
				throw 'Could not locate any renderers';
			}
			renderer = new renderer({
				'container'  : rendererNode,
				'gridConfig' : gridConfig
			});
		}else if(!(renderer.prototype instanceof mapapi['renderer'])){
			throw 'Specified renderer is not an instance of mapapi.renderer';
		}else{
			renderer = new renderer({
				'container' : rendererNode,
				'gridConfig' : gridConfig
			});
		}
		empty(container);
		container['appendChild'](rendererNode);
		container['appendChild'](sidebarsContainer);

		addClass(container, 'mapapi-ui');
		if(this['name']){
			addClass(container, 'mapapi-ui-' + this['name']['toLowerCase']()['replace'](/\s+/g,''));
		}
		addClass(rendererNode, 'mapapi-ui-renderer');
		addClass(sidebarsContainer, 'mapapi-ui-sidebars');

		obj['renderer']          = renderer;
		obj['rendererNode']      = rendererNode;
		obj['contentNode']       = container;
		obj['sidebars']          = {};
		obj['sidebarLabels']     = [];
		obj['sidebarsContainer'] = sidebarsContainer;
		obj['markerManager']     = markerMngr;
		obj['ID']                = uiID++;

		obj['loadCSS']();
	}

	ui.prototype['css'] = [
		'reset.css',
		'ui.css'
	];

	ui.prototype['loadCSS'] = function(){
		var
			obj     = this,
			head    = document.getElementsByTagName('head')[0],
			scripts = head.getElementsByTagName('script'),
			links   = head.getElementsByTagName('link'),
			regexp  = /./,
			uiregex = /(mapapi\.ui\.js|mapapi-complete.js)$/,
			exregex = /^https?/,
			styles  = [],
			css     = [],
			csspath,
			csspathregex,
			mapuijs,
			cssfound,
			newcss,
			jspath
		;
		for(var i=0;i<links.length;++i){
			if(/\bstylesheet\b/.test(links[i]['rel'])){
				styles.push(links[i]);
			}
		}
		links = [];
		for(var i=0;i<scripts.length;++i){
			if(uiregex.test(scripts[i]['src'])){
				mapuijs = scripts[i]['src'];
			}
		}
		if(mapuijs == undefined){
			throw 'Could not find mapapi.js UI file';
		}else{
			jspath = mapuijs['replace'](uiregex,'');
			for(var i=0;i<ui.prototype.css.length;++i){
				css.push(ui.prototype.css[i]);
			}
			for(var i=0;i<obj['css']['length'];++i){
				css.push(obj['css'][i]);
			}
			for(var i=0;i<css.length;++i){
				cssfound     = false;
				csspath      = css[i];
				csspathregex = csspath.replace(/\./g,'\.').replace(/\//g,'\/');
				if(exregex.test(csspath)){
					regexp.compile('/^' + csspathregex + '$/');
				}else{
					regexp.compile('^' + jspath.replace(/\./g,'\.').replace(/\//g,'\/') + csspathregex + '$');
				}
				for(var j=0;j<styles.length;++j){
					if(regexp.test(styles[j]['href'])){
						cssfound = true;
						break;
					}
				}
				if(!cssfound){
					newcss = createElement('link');
					newcss['setAttribute']('rel','stylesheet');
					newcss['setAttribute']('type','text/css');
					newcss['setAttribute']('href',exregex.test(csspath) ? csspath : mapuijs['replace'](uiregex,csspath));
					head['appendChild'](newcss);
				}
			}
		}
	}

	ui.prototype['addSidebar'] = function(sidebarName, sidebarObj){
		if(typeof sidebarName != 'string'){
			throw 'sidebar name should be specified as string';
		}else if(!(sidebarObj instanceof sidebar)){
			throw 'sidebar object should be an instanceof mapapi.ui.sidebar';
		}
		sidebarName = sidebarName['replace'](trimRegex,'');
		if(sidebarName == ''){
			throw 'sidebar name is empty';
		}
		var
			lname = sidebarName['toLowerCase']()
		;
		if(this['sidebars'][lname] != undefined){
			throw 'A sidebar with that name has already been assigned';
		}else{
			var
				DOM = this['sidebar2DOM'](sidebarName, sidebarObj)
			;
			this['sidebars'][lname] = sidebarObj;
			this['sidebarLabels']['push'](lname);
			this['sidebarsContainer']['appendChild'](DOM);
			mapapi['events']['fire']('sidebaradded',{'ui':this,'sidebar':sidebarObj,'DOM':DOM,'name':sidebarName});
			return DOM;
		}
	}

	function sectionsAddedListener(e){
		var
			sections = e['sections']
		;
		if(sections && sections instanceof Array){
			for(var i=0;i<sections['length'];++i){
				if(sections[i] instanceof section){
					var
						li         = createElement('li'),
						h1         = createElement('h1'),
						ul         = createElement('ul'),
						subsection = sections[i],
						text       = subsection['text']()
					;
					h1['appendChild'](createText(text));
					h1['onclick'] = function(){
						toggleClass(this['parentNode'], 'toggled');
						subsection['fire']('click');
					};
					li['appendChild'](h1);
					li['appendChild'](ul);
					addClass(li, text['toLowerCase']()['replace'](/[^A-z\d]+/g,''));
					addClass(li, 'childless');
					subsection['DOM'] = ul;
					this['DOM']['appendChild'](li);
					delClass(this['DOM']['parentNode'], 'childless');
					subsection['addListener']('sectionsadded', sectionsAddedListener);
					subsection['addListener']('sectionsremoved', sectionsRemovedListener);
				}
			}
		}
	}
	function sectionsRemovedListener(e){
		var
			sections = e['sections'],
			parents  = []
		;
		if(sections && sections instanceof Array){
			for(var i=0;i<sections['length'];++i){
				var
					DOM    = sections[i]['DOM']['parentNode'],
					parent = DOM['parentNode']
				;
				if(parents['indexOf'](parent) == -1){
					parents.push(parent);
				}
				parent['removeChild'](DOM);
			}
			for(var i=0;i<parents.length;++i){
				if(!parents[i]['hasChildNodes']()){
					addClass(parents[i], 'childless');
				}
			}
		}
	}
	ui.prototype['sidebar2DOM'] = function(sidebarName, sidebarObj){
		if(typeof sidebarName != 'string'){
			throw 'sidebar name should be specified as string';
		}else if(!(sidebarObj instanceof sidebar)){
			throw 'sidebar object should be an instanceof mapapi.ui.sidebar';
		}
		sidebarName = sidebarName['replace'](trimRegex,'');
		if(sidebarName == ''){
			throw 'sidebar name is empty';
		}
		var
			obj = this,
			li  = createElement('li'),
			ul  = createElement('ul')
		;
		addClass(li, sidebarName['toLowerCase']()['replace'](/\s+/g,''));
		li['appendChild'](ul);
		sidebarObj['DOM'] = ul;
		sidebarObj['addListener']('sectionsadded', sectionsAddedListener);
		sidebarObj['addListener']('sectionsremoved', sectionsRemovedListener);
		return li;
	}

	ui.prototype['sidebar'] = function(index, disableFallback){
		if(typeof disableFallback != 'boolean'){
			disableFallback = false;
		}else if(typeof index != 'number'){
			throw 'Sidebar indexes must be specified as integers';
		}
		var
			index        = Math.floor(index),
			labelLengths = this['sidebarLabels']['length']
		;
		if(labelLengths >= 1){
			if(index < 0){
				throw 'Negative indexes are not supported';
			}else if(index >= labelLengths){
				if(!disableFallback){
					return this['sidebars'][this['sidebarLabels'][labelLengths - 1]];
				}
			}else if(this['sidebarLabels'][index] != undefined){
				return this['sidebars'][this['sidebarLabels'][index]];
			}
		}
		return false;
	}

	mapapi['ui'] = ui;
	mapapi['userinterfaces'] = {};

	function uiItem(){
		if(EventTarget == undefined){
			throw 'EventTarget not loaded';
		}
		var
			obj        = this,
			DOMclasses = obj['DOMclasses']
		;
		EventTarget['call'](obj);

		obj['opts'] = {
			'open' : false
		};

		obj['rendererEvents'] = {
			'focus_changed'  : [],
			'bounds_changed' : []
		};

		obj['DOM'] = undefined;
		obj['addListener']('content_changed', function(){
			if(!obj['DOM']){
				obj['DOM'] = obj['content2DOM']();
				for(var i=0;i<DOMclasses['length'];++i){
					addClass(obj['DOM'], DOMclasses[i]);
				}
			}else{
				for(var i=0;i<obj['DOM']['childNodes']['length'];++i){
					obj['DOM']['removeChild'](obj['DOM']['childNodes'][i]);
				}
				var
					newDOM = obj['content2DOM']()['childNodes']
				;
				for(var i=0;i<newDOM['length'];++i){
					obj['DOM']['appendChild'](newDOM[i]);
				}
			}
		});
	}
	extend(uiItem, EventTarget);

	uiItem.prototype['DOMclasses'] = [
		'mapapi-ui-item'
	];

	uiItem.prototype['position'] = function(position){
		var
			opts = this['opts']
		;
		if(position != undefined){
			if((position instanceof gridPoint) == false && typeof position['x'] == 'number' &&  typeof position['y'] == 'number'){
				position = new gridPoint(position['x'], position['y']);
			}
			if((position instanceof gridPoint) == false){
				throw 'No position specified';
			}
			opts['position'] = position;
			this['fire']('position_changed');
		}
		return opts['position'];
	}

	uiItem.prototype['hide'] = function(){
		if(this['DOM'] && this['DOM']['parentNode']){
			this['DOM']['style']['display'] = 'none';
		}
	}

	uiItem.prototype['show'] = function(){
		if(this['DOM'] && this['DOM']['parentNode']){
			this['DOM']['style']['display'] = 'block';
		}
	}

	uiItem.prototype['content'] = function(content){
		var
			opts     = this['opts']
		;
		if(content != undefined){
			if(typeof content != 'string' && !content['appendChild'] && !(content instanceof Image)){
				throw 'Contents are invalid';
			}
			opts['content'] = content;
			this['fire']('content_changed');
		}
		return opts['content'];
	}

	uiItem.prototype['content2DOM'] = function(){
		var
			obj = this,
			content = obj['content'](),
			content = content == undefined ? '' : content,
			DOM     = createElement('div')
		;

		if(typeof content == 'string'){
			var
				paragraphs,
				paragraph
			;
			paragraphs = /\n/.test(content) ? content.split("\n") : [content];
			for(var i=0;i<paragraphs.length;++i){
				paragraph = createElement('p');
				paragraph.appendChild(createText(paragraphs[i]));
				DOM.appendChild(paragraph);
			}
		}else if(content['appendChild'] != undefined || content instanceof Image){
			DOM.appendChild(content);
		}

		addClass(DOM, 'mapapi-ui-item-contents');

		return DOM;
	}

	uiItem.prototype['csspos'] = function(){
		return this['ui']['renderer']['point2px'](this['position']());
	}

	uiItem.prototype['open'] = function(ui){
		if(ui == undefined){
			throw 'UI was not supplied';
		}else if((ui instanceof mapapi['ui']) == false){
			throw 'ui argument is not an instance of mapapi.ui';
		}else if(this['DOM'] != undefined && this['DOM']['parentNode'] != undefined && this['DOM']['parentNode'] == ui['contentNode']){
			throw 'Already open';
		}else if(this['ui'] != undefined && this['ui'] != ui){
			throw 'Already open on another UI instance';
		}
		this['ui'] = ui;
		var
			obj     = this,
			DOM     = obj['DOM'],
			DOMp    = DOM ? (DOM['parentNode'] == undefined ? undefined : DOM['parentNode']) : undefined,
			dest    = ui['contentNode']
		;
		if(DOM != undefined){
			if(!!obj['opts']['autoFocus']){
				ui['renderer']['focus'](obj['position']);
			}
			dest['appendChild'](DOM);
			var
				renderer = ui['renderer'],
				rcontent = renderer['contentNode'],
				offset   = function(){
					if(!!(DOM ? (DOM['parentNode'] == undefined ? undefined : DOM['parentNode']) : undefined)){
						var
							style     = DOM['style'],
							wasHidden = (style['display'] == 'none'),
							zIndex    = style['zIndex']
						;
						if(wasHidden){
							style['zIndex'] = '-1';
							style['display'] = 'block';
							style['left'] = 0;
							style['top'] = 0;
						}
						var
							csspos      = obj['csspos'](),
							height      = DOM['clientHeight'],
							width       = DOM['clientWidth'],
							top         = csspos['y'],
							left        = csspos['x'],
							contentNode = ui['renderer']['contentNode'],
							vertical    = height > 0 && top >= 0  && (top + height) <= contentNode['clientHeight'],
							horizontal  = width > 0  && left >= 0 && (left + DOM['clientWidth']) <= contentNode['clientWidth']
						;
						if((vertical && horizontal) || (height == 0 && obj['ui']['renderer']['bounds']()['isWithin'](obj['position']()))){
							style['top']  = top + 'px';
							style['left'] = left + 'px';
							if(obj['opts']['disableAutoShow'] != true){
								obj['show']()
							}else{
								obj['hide']();
							}
						}else{
							obj['hide']();
						}
						if(wasHidden){
							if(zIndex == undefined){
								delete style['zIndex'];
							}else{
								style['zIndex'] = zIndex;
							}
						}
					}
				}
			;
			offset();
			obj['rendererEvents']['focus_changed' ].push('focus_changed' , ui['renderer']['addListener']('focus_changed' , offset));
			obj['rendererEvents']['bounds_changed'].push('bounds_changed', ui['renderer']['addListener']('bounds_changed', offset));
			obj['fire']('opened');
		}
		obj['opts']['open'] = true;
	}

	uiItem.prototype['close'] = function(){
		var
			obj    = this,
			DOM    = obj['DOM'],
			DOMp   = (DOM != undefined) ? DOM['parentNode'] : undefined,
			ui     = obj['ui'],
			events = obj['rendererEvents']
		;
		if(DOM != undefined && DOMp != undefined){
			DOMp['removeChild'](DOM);
		}
		if(ui && ui['renderer']){
			for(var type in events){
				for(var i=0;i<events[type].length;++i){
					ui['renderer']['removeListener'](type, events[i]);
				}
			}
		}
		obj['fire']('closed');
		obj['opts']['open'] = false;
	}

	function infoWindow(options){
		uiItem['call'](this);
		var
			obj       = this,
			opts      = {},
			options   = options || {},
			content   = options['content'],
			autoFocus = options['autoFocus'] == undefined ? true : !!options['autoFocus'],
			maxWidth  = Math.max(80, options['maxWidth'] || 0),
			position  = options['position'],
			zIndex    = options['zIndex'] || 0
		;

		obj['opts'] = opts;

		obj['content'](content);
		obj['position'](position);
		obj['maxWidth'](maxWidth);
		obj['zIndex'](zIndex);

		obj['autoFocus'] = autoFocus;

		obj['DOM'] = undefined;

		if(content != undefined){
			obj['fire']('content_changed');
		}
	};
	extend(infoWindow, uiItem);

	infoWindow.prototype['csspos'] = function(){
		var pos = uiItem.prototype['csspos']['call'](this);
		pos['y'] -= (this['DOM'] != undefined && this['DOM']['clientHeight'] != undefined) ? this['DOM']['clientHeight'] : 0;
		return pos;
	}

	infoWindow.prototype['maxWidth'] = function(maxWidth){
		var
			opts = this['opts']
		;
		if(maxWidth != undefined){
			if(typeof maxWidth != 'number'){
				throw 'max width should be a number';
			}
			opts['maxWidth'] = Math.max(80, maxWidth || 0);
		}
		return opts['maxWidth'];
	}

	infoWindow.prototype['zIndex'] = function(zIndex){
		var
			opts = this['opts']
		;
		if(zIndex != undefined){
			if(typeof zIndex != 'number'){
				throw 'zIndex should be number';
			}
			opts['zIndex'] = zIndex;
			this['fire']('zIndex_changed');
		}
		return opts['zIndex'];
	}

	infoWindow.prototype['content2DOM'] = function(){
		var
			obj     = this,
			content = uiItem.prototype['content2DOM']['call'](obj),
			DOM     = createElement('aside'),
			div     = createElement('div'),
			close   = createElement('p')
		;
		addClass(close, 'mapapi-ui-infowindow-close');
		addClass(div,   'mapapi-ui-wrapper');
/*
		obj['addListener']('opened', function(){
			setTimeout(function(){
				addClass(div, 'done');
			},100);
		});
		obj['addListener']('closed', function(){
			delClass(div, 'done');
		});
*/

		close['appendChild'](createText('×'));
		close['setAttribute']('title', 'Close');
		div.appendChild(content);
		content.appendChild(close);
		if(obj['maxWidth']() != undefined){
			DOM['style']['maxWidth'] = obj['maxWidth']();
		}
		close['onclick'] = function(){
			obj['close']();
		}
		DOM['appendChild'](div);
		return DOM;
	}

	infoWindow.prototype['DOMclasses'] = [
		'mapapi-ui-infowindow'
	];

	mapapi['infoWindow'] = infoWindow;
	ui.prototype['infoWindow'] = function(options){
		return new infoWindow(options);
	}

	function marker(options){
		uiItem['call'](this);
		if(options == undefined){
			return;
		}
		if(Image == undefined){
			throw 'Your browser does not support the image object';
		}
		var
			obj      = this,
			opts     = obj['opts'],
			options  = options || {},
			image    = options['image'],
			anchor   = options['anchor'],
			position = options['position'],
			infoW    = options['infoWindow'],
			img      = new Image
		;
		if(image == undefined){
			throw 'No marker image specified';
		}else if(position == undefined){
			throw 'No position specified';
		}else if((position instanceof gridPoint) == false){
			throw 'Invalid position specified';
		}
		obj['position'](position);
		if(anchor != undefined){
			obj['anchor'](anchor);
		}
		obj['position'](position);
		img['onload'] = function(){
			if(anchor == undefined){
				obj['anchor']({'x':img['width'] / 2, 'y' : img['height']});
			}
			obj['content'](img);
		}
		img['onerror'] = function(){
			throw 'Could not load image';
		}
		img['src'] = image;
		obj['img'] = img;
		if(infoW instanceof infoWindow){
			infoW['position'](obj['position']());
			obj['addListener']('click', function(){
				obj['opts']['disableAutoShow'] = true;
				obj['hide']();
				infoW['open'](obj['ui']);
			});
			infoW['addListener']('closed', function(){
				obj['opts']['disableAutoShow'] = false;
				obj['show']();
			});
		}
	}
	extend(marker, uiItem);

	marker.prototype['anchor'] = function(anchor){
		if(anchor != undefined){
			if(typeof anchor['x'] != 'number' || typeof anchor['y'] != 'number'){
				throw 'x and y anchor points must be numbers';
			}
			this['opts']['anchor'] = {'x':anchor['x'], 'y':anchor['y']};
		}
		return this['opts']['anchor'];
	}

	marker.prototype['csspos'] = function(){
		var
			pos    = uiItem.prototype['csspos']['call'](this),
			anchor = this['anchor']()
		;
		if(anchor == undefined){
			throw 'No anchor point found';
		}else{
			pos['x'] -= anchor['x'],
			pos['y'] -= anchor['y']
		}
		return pos;
	}

	marker.prototype['content2DOM'] = function(){
		var
			obj     = this,
			content = obj['content'](),
			DOM     = createElement('img')
		;
		if(!(content instanceof Image) && content['nodeName']['toLowerCase']() != 'img'){
			throw 'Invalid contents, must be an instance of Image or an img tag';
		}
		DOM['setAttribute']('src', content['src']);
		DOM['onclick'] = function(){
			obj['fire']('click');
		}
		return DOM;
	}

	marker.prototype['DOMclasses'] = [
		'mapapi-ui-marker'
	];

	mapapi['marker'] = marker;

	function markerManager(){
		EventTarget['call'](this);
		this['markers'] = [];
	}
	extend(markerManager, EventTarget);

	markerManager.prototype['add'] = function(one){
		if(one == undefined){
			throw 'No marker specified';
		}else if(one instanceof marker){
			if(this['markers']['indexOf'](one) == -1){
				this['markers']['push'](one);
			}
		}else{
			throw 'value is not a marker';
		}
	}

	markerManager.prototype['remove'] = function(one){
		if(one != undefined){
			var
				pos = this['markers']['indexOf'](one)
			;
			if(pos >= 0){
				this['markers'][i]['close']();
				this['markers']['splice'](pos, 1);
			}
		}
	}

	markerManager.prototype['open'] = function(on){
		if(!(on instanceof ui)){
			throw 'value must be an instance of mapapi.ui';
		}
		for(var i=0;i<this['markers']['length'];++i){
			this['markers'][i]['open'](on);
		}
		this['fire']('opened');
	}

	markerManager.prototype['close'] = function(){
		for(var i=0;i<this['markers']['length'];++i){
			this['markers'][i]['close']();
		}
		this['fire']('closed');
	}

	mapapi['markerManager'] = markerManager;
	ui.prototype['addMarker'] = function(one){
		if(one instanceof marker){
			this['markerManager']['add'](one);
		}else{
			throw 'value must be instance of mapapi.marker';
		}
	}

	function numberedMarker(options){
		marker['call'](this, options);
		if(options == undefined){
			return;
		}
		this['opts']['number'] = options['number'] || 0;
	}
	extend(numberedMarker, marker);

	numberedMarker.prototype['number'] = function(number){
		if(typeof number == 'number'){
			this['opts']['number'] = number;
			this['fire']('content_changed');
		}
		return this['opts']['number'];
	}

	numberedMarker.prototype['content2DOM'] = function(){
		var
			obj     = this,
			content = marker.prototype['content2DOM']['call'](obj),
			img     = obj['img'],
			DOM     = createElement('div'),
			number  = createElement('p'),
			value   = parseInt(obj['opts']['number'])
		;
		number['appendChild'](createText(value));
		number['setAttribute']('title', value);
		number['onclick'] = function(){
			obj['fire']('click');
		}

		delClass(content, 'mapapi-ui-marker');
		addClass(content, 'mapapi-ui-marker-img');

		if(!!img){
			DOM['style']['width']  = img['width'] + 'px';
			DOM['style']['height'] = img['height'] + 'px';
		}

		addClass(number, 'mapapi-ui-marker-number');

		DOM['appendChild'](content);
		DOM['appendChild'](number);

		return DOM;
	}
	numberedMarker.prototype['DOMclasses'] = [
		'mapapi-ui-marker',
		'mapapi-ui-marker-numbered'
	];

	mapapi['numberedMarker'] = numberedMarker;

	var
		sectionID = 0
	;

	function sidebar(){
		EventTarget['call'](this);
		this['sections'] = [];
		this['DOM']      = undefined;
		this['ID'] = sectionID++;
	}

	extend(sidebar, EventTarget);

	sidebar.prototype['addSection'] = function(){
		var
			addThese = []
		;
		for(var i=0;i<arguments['length'];++i){
			if(!(arguments[i] instanceof section || (arguments[i].prototype != undefined && arguments[i].prototype instanceof section))){
				throw 'sub-section should be instanceof mapapi.ui.section';
			}else if(this['sections']['indexOf'](arguments[i]) == -1){
				addThese['push'](arguments[i]);
			}
		}
		if(addThese['length'] > 0){
			this['sections']['push']['apply'](this['sections'], addThese);
			this['fire']('sectionsadded',{'sections':addThese});
		}
	}

	sidebar.prototype['removeSection'] = function(){
		var
			removeThese = [],
			pos
		;
		for(var i=0;i<arguments['length'];++i){
			if(!(arguments[i] instanceof section || (arguments[i].prototype != undefined && arguments[i].prototype instanceof section))){
				throw 'sub-section should be instanceof mapapi.ui.section';
			}else if(this['sections']['indexOf'](arguments[i]) > -1){
				removeThese['push'](arguments[i]);
			}
		}
		if(removeThese['length'] > 0){
			for(var i=0;i<removeThese['length'];++i){
				this['sections']['splice'](this['sections']['indexOf'](removeThese[i]), 1);
			}
			this['fire']('sectionsremoved',{'sections':removeThese});
		}
	}

	ui['sidebar'] = sidebar;

	function section(options){
		sidebar['call'](this);
		this['opts'] = {};
		if(options != undefined){
			if(typeof options == 'string'){
				options = {'text':options};
			}
			this['options'](options);
		}
		this['DOM'] = undefined;
	}

	extend(section, sidebar);

	section.prototype['options'] = function(options){
		var
			options = options || {},
			opts    = this['opts'],
			text    = options['text']
		;
		if(text != undefined){
			if(text + '' != text){
				throw 'text should be a string!';
			}
			text = text['replace'](/^\s+|\s+$/,'');
			if(text == ''){
				throw 'text is empt!';
			}
			if(text != opts['text']){
				this['fire']('changedtext');
				opts['text'] = text;
			}
		}
	}

	section.prototype['text'] = function(text){
		if(text != undefined){
			this['options']({'text':text});
		}
		return this['opts']['text'];
	}

	ui['section'] = section;
})(window);
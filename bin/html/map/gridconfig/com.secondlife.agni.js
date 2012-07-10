/**
* @license License and Terms of Use
*
* Copyright (c) 2011 SignpostMarv
* Copyright (c) 2010 Linden Research, Inc.
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
		document       = window['document'],
		mapapi         = window['mapapi'],
		IDBTransaction = window['IDBTransaction'],
		Date           = window['Date'],
		console        = window['console'],
		gridConfig     = mapapi['gridConfig'],
		tileSource     = mapapi['tileSource'],
		size           = mapapi['size'],
		gridPoint      = mapapi['gridPoint']
	;

	mapapi['gridConfigs'] = mapapi['gridConfigs'] || {};

	var SecondLifeTileSource = new tileSource({
		'copyright'       : '© 2007 - ' + (new Date).getFullYear() + ' Linden Lab',
		'label'           : 'Land & Objects',
		'maxZoom'         : 7,
		'backgroundColor' : '#1d475f'
	});

//  This Function returns the appropriate image tile from the S3 storage site corresponding to the
//  input location and zoom level on the google map.
	SecondLifeTileSource['getTileURL'] = function(pos, zoom){
		var
			sl_zoom               = Math.floor(zoom + 1),
			regions_per_tile_edge = Math.pow(2, sl_zoom - 1),
			x                     = pos['x'],
			y                     = pos['y']
		;
		x -= x % regions_per_tile_edge;
		y -= y % regions_per_tile_edge; 

		if(x < 0 || y < 0){
			return null;
		}
		return (
			[ // 2 hosts so that we get faster performance on clients with lots of bandwidth but possible browser limits on number of open files
				"http://map.secondlife.com.s3.amazonaws.com",
				"http://map.secondlife.com"
			][((x / regions_per_tile_edge) % 2)] //  Pick a server
			+ ["/map", sl_zoom, x, y, "objects.jpg"].join("-") //  Get image tiles from Amazon S3
		);
	}

	var
		pos2region_pool = 0,
		region2pos_pool = 0,
		agni            = new gridConfig({
			'namespace'   : 'com.secondlife.agni',
			'vendor'      : 'Linden Lab',
			'name'        : 'Second Life',
			'description' : 'Linden Lab\'s Agni grid',
			'label'       : 'Agni',
			'size'        : new size(1048576, 1048576),
			'tileSources' : [
				SecondLifeTileSource
			],
			'maxZoom'     : 7,
			'pos2region'  : function(pos, success, fail){
				if(!(pos instanceof mapapi['gridPoint'])){
					throw 'Position should be an instance of mapapi.gridPoint';
				}
				function noIndexedDB(p, s, f){
					if(console != undefined){
						console.log('IndexedDB cache miss @ ' + Math.floor(p['x']) + ',' + Math.floor(p['y']));
					}
					var
						cachecheck = agni['apiCacheCheck']('pos2region', Math.floor(p['x']), Math.floor(p['y']));
					;
					if(cachecheck != undefined){
						if(s){
							s({'pos':p, 'region': cachecheck});
						}
						return;
					}
					var
						script = document['createElement']('script'),
						_var   = 'com_secondlife_agni_posToRegion_' + ((++pos2region_pool) + '')['replace'](/\d/g,function(a){ return 'ABCDEFGHIJ'[a]; })
					;
					function done(){
						if(window[_var] == undefined){
							if(f){
								f('slurl.com API failed to load script variable');
							}
						}else if(window[_var]['error'] != undefined){
							if(f){
								f('slurl.com API call failed, perhaps your arguments were invalid');
							}
						}else{
							var
								region = window[_var] + ''
							;
							if(s){
								s({'pos':p, 'region': region,'cache':false});
							}
							if(agni['IndexedDB']){
								var
									transaction = agni['IndexedDB']['transaction']('pos2region', IDBTransaction['READ_WRITE']),
									objstore    = transaction['objectStore']('pos2region'),
									req         = objstore['add']({
										'pos'      : Math.floor(p['x']) + '_' + Math.floor(p['y']),
										'region'   : region,
										'l_region' : region['toLowerCase'](),
										'cached'   : Math.floor(new Date()['getTime']() / 1000)
									})
								;
								if(console != undefined){
									req['onsuccess'] = function(){
										console.log('IndexedDB cache write @ ' + Math.floor(p['x']) + ',' + Math.floor(p['y']) + ' for ' + region);
									}
								}
							}
							agni['APIcache']['pos2region'][Math.floor(p['x'])]                     = agni['APIcache']['pos2region'][Math.floor(p['x'])] || {};
							agni['APIcache']['pos2region'][Math.floor(p['x'])][Math.floor(p['y'])] = window[_var] + '';
							script['parentNode']['removeChild'](script);
						}
					}
					script['onload'] = done;
					script['onreadystatechange'] = function(){
						if(script['readyState'] == 'complete' || script['readyState'] == 'loaded'){
							done();
						}
					}
					script['onerror'] = function(){
						if(f){
							f('Error with script loading the slurl.com API');
						}
						setTimeout(function(){
							script['parentNode']['removeChild'](script);
						},30000);
					}
					script['setAttribute']('src', 'http://slurl.com/get-region-name-by-coords?' + ['var=' + escape(_var), 'grid_x=' + escape(Math.floor(p['x'])), 'grid_y=' + escape(Math.floor(p['y']))].join('&'));
					document['getElementsByTagName']('head')[0]['appendChild'](script);
				}
				if(agni['IndexedDB']){
					var
						index = agni['IndexedDB']['transaction']('pos2region')['objectStore']('pos2region')
					;
					index['get'](Math.floor(pos['x']) + '_' + Math.floor(pos['y']))['onsuccess'] = function(e){
						if(e['target']['result']){
							if(console != undefined){
								console.log('IndexedDB cache hit @ ' + Math.floor(pos['x']) + ',' + Math.floor(pos['y']));
							}
							if(success){
								success({'pos':pos, 'region':e['target']['result']['region'],'cache':'hit'});
							}
						}else{
							noIndexedDB(pos, success, fail);
						}
					}
				}else{
					noIndexedDB(pos, success, fail);
				}
			},
			'region2pos' : function(region, success, fail){
				var
					script = document['createElement']('script'),
					_var   = 'com_secondlife_agni_regionTopos_' + ((++region2pos_pool) + '')['replace'](/\d/g,function(a){ return 'ABCDEFGHIJ'[a]; })
				;
				function noIndexedDB(r, s, f){
					function done(){
						if(window[_var] == undefined){
							if(f){
								f('slurl.com API failed to load script variable');
							}
						}else if(window[_var]['error'] != undefined){
							if(f){
								f('slurl.com API call failed, perhaps your arguments were invalid');
							}
						}else{
							var
								pos = window[_var]
							;
							if(s){
								s({'pos':gridPoint['fuzzy'](pos), 'region': r, 'cache':false});
							}
							if(agni['IndexedDB']){
								agni['pos2region'](pos);
							}
							script['parentNode']['removeChild'](script);
						}
					}
					script['onload'] = done;
					script['onreadystatechange'] = function(){
						if(script['readyState'] == 'complete' || script['readyState'] == 'loaded'){
							done();
						}
					}
					script['onerror'] = function(){
						if(f){
							f('Error with script loading the slurl.com API');
						}
						setTimeout(function(){
							script['parentNode']['removeChild'](script);
						},30000);
					}
					script['setAttribute']('src', 'http://slurl.com/get-region-coords-by-name?' + ['var=' + escape(_var), 'sim_name=' + escape(r)].join('&'));
					document['getElementsByTagName']('head')[0]['appendChild'](script);
				}
				if(agni['IndexedDB']){
					var
						req = agni['IndexedDB']['transaction']('pos2region')['objectStore']('pos2region')['index']('l_region')['get'](region['toLowerCase']())
					;
					req['onsuccess'] = function(e){
						if(e['target']['result']){
							if(console != undefined){
								console.log('IndexedDB cache hit @ ' + region['toLowerCase']());
							}
							var
								pos = e['target']['result']['pos']['split']('_')
							;
							if(success){
								success({'pos':gridPoint['fuzzy']({'x':pos[0],'y':pos[1]}),'region':e['target']['result']['region'],'cache':'hit'})
							}
						}else{
							noIndexedDB(region, success, fail);
						}
					}
					req['onerror'] = function(e){
						console.log('IndexedDB cache miss @ ' + region['toLowerCase']());
						noIndexedDB(region, success, fail);
					}
				}else{
					noIndexedDB(region, success, fail);
				}
			}
		})
	;

	mapapi['gridConfigs']['com.secondlife.agni'] = agni;
})(window);
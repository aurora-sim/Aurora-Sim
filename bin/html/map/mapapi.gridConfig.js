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
		document       = window['document'],
		mapapi         = window['mapapi'],
		IndexedDB      = window['IndexedDB'],
		IDBTransaction = window['IDBTransaction'],
		Date           = window['Date'],
		cacheTimeout   = {
			'pos2region' : 86400
		}
	;

	function useDatabase(db, grdcnfg){
		grdcnfg['IndexedDB'] = db;
		if(db['objectStoreNames']['contains']('pos2region')){
			var
				deleteThese = [],
				now         = Math.floor(new Date()['getTime']() / 1000)
			;
			db['transaction']('pos2region', IDBTransaction['READ'])['objectStore']('pos2region')['openCursor']()['onsuccess'] = function(e){
				var
					cursor = e['target']['result'],
					value  = cursor ? cursor['value'] : undefined
				;
				if(cursor){
					if(value !== undefined && (value['cached'] == undefined || typeof value['cached'] != 'number' || (now - value['cached']) >= cacheTimeout['pos2region'])){
						deleteThese.push(value['pos']);
					}
					cursor['continue']();
				}else{
					if(deleteThese.length){
						var i=0;
						function a(){
							if(i < deleteThese.length){
								b();
							}
						}
						function b(){
							db['transaction']('pos2region', IDBTransaction['READ_WRITE'])['objectStore']('pos2region')['delete'](deleteThese[i++])['onsuccess'] = a;
						}
						a();
					}
				}
			}
		}
		db['onversionchange'] = function(e){
			db['close']();
			alert('A new version of this page is ready. Please reload!');
		}
	}

	function gridConfig(options){
		var
			obj     = this,
			options = options || {}
		;
		obj['namespace']    = options['namespace'];
		obj['vendor']       = options['vendor'];
		obj['name']         = options['name'];
		obj['description']  = options['description'] || 'No description specified';
		obj['label']        = options['label'];
		obj['maxZoom']      = options['maxZoom'];
		obj['size']         = options['size'] || new mapapi['size'](options['gridWidth'] || 1048576, options['gridHeight'] || 1048576);
		obj['tileSources']  = options['tileSources'] || [];

		obj['API']          = {};
		obj['APIcache']     = {};
		if(options['pos2region'] != undefined){
			obj['API']['pos2region'] = options['pos2region'];
			obj['APIcache']['pos2region'] = {};
		}
		if(options['region2pos'] != undefined){
			obj['API']['region2pos'] = options['region2pos'];
		}

		var
			needsIndexedDB  = false,
			needsIndexedDBc = [options['pos2region'] != undefined]
		;

		for(var i=0;i<needsIndexedDBc.length;++i){
			if(needsIndexedDBc[i] == true){
				needsIndexedDB = true;
				break;
			}
		}

		if(IndexedDB != undefined && needsIndexedDB){
			try{
				var
					idb = IndexedDB['open'](obj['namespace'])['onsuccess'] = function(e){
						var
							db = e['target']['result']
						;
						if(db['version'] == '0.1'){
							useDatabase(db, obj);
							return;
						}else if(db['version'] != ''){
							obj['IndexedDB'] = undefined;
							throw 'Database version is not upgradable';
						}
						var
							req = db['setVersion']('0.1')
						;
						req['onblocked'] = function(e){
							obj['IndexedDB'] = undefined;
							throw 'Site is open in other tabs, please close them so the database can be upgraded';
						}
						req['onsuccess'] = function(e){
							var
								pos2region = db['createObjectStore']('pos2region', {'keyPath':'pos'})
							;
							pos2region['createIndex']('region', 'region', {'unique': false});
							pos2region['createIndex']('l_region', 'l_region', {'unique': false});
							useDatabase(db, obj);
						}
					}
				;
			}catch(e){
				if(e['name'] != 'NS_ERROR_DOM_INDEXEDDB_UNKNOWN_ERR'){
					throw e;
				}else if(window['console'] != undefined){
					console.log('IndexedDB failure. Are you running locally in Firefox? Try debugging in chrome, or load on a localhost web server');
				}
			}
		}
	}

	gridConfig.prototype['pos2region'] = function(pos, success, fail){
		var
			error
		;
		if(this['API']['pos2region'] == undefined){
			error = 'This grid config has no pos2region API';
		}else if(pos == undefined){
			error = 'Position was not specified';
		}
		if(error == undefined){
			this['API']['pos2region'](mapapi['gridPoint']['fuzzy'](pos), success, fail);
		}else if(fail != undefined){
			fail(error);
		}else{
			throw error;
		}
	}

	gridConfig.prototype['region2pos'] = function(region, success, fail){
		var
			error
		;
		if(this['API']['region2pos'] == undefined){
			error = 'This grid config has no region2pos API';
		}else if(region == undefined){
			error = 'Region was not specified';
		}
		if(error == undefined){
			this['API']['region2pos'](region, success, fail);
		}else if(fail){
			fail(error);
		}else{
			throw error;
		}
	}

	gridConfig.prototype['apiCacheCheck'] = function(call){
		var
			obj  = this,
			dest = obj['APIcache'][call],
			i = 1
		;
		if(dest != undefined && arguments.length >= 2){
			for(i=1;i<arguments.length;++i){
				dest = dest[arguments[i]];
				if(dest == undefined){
					break;
				}
			}
			if(i == arguments.length && dest != undefined){
				return dest;
			}
		}
		return undefined;
	}

	mapapi['gridConfig'] = gridConfig;
})(window);
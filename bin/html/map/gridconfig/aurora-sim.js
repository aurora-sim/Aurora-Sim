/**
* @license License and Terms of Use
*
* Copyright (c) 2012 SignpostMarv
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
		Date           = window['Date'],
		gridConfig     = mapapi['gridConfig'],
		tileSource     = mapapi['tileSource'],
		size           = mapapi['size'],
		gridPoint      = mapapi['gridPoint']
	;

	mapapi['gridConfigs'] = mapapi['gridConfigs'] || {};

	mapapi['gridConfigs']['aurorasim'] = function(obj){
		var
			tileLabel       = obj['tileLabel'] || 'Land & Objects',
			minZoom         = obj['minZoom'] || 0,
			maxZoom         = obj['maxZoom'] || 7
			backgroundColor = obj['backgroundColor'] || '#1d475f',
			size            = new size(obj['gridWidth'] || 1048576, obj['gridHeight'] || 1048576)
			gridLookup      = obj['gridLookup'] || {},
			pos2region      = {},
			region2pos      = {},
			mapTextureURL   = obj['mapTextureURL'] || false,
			namespace       = obj['namespace'] || false,
			vendor          = obj['vendor'] || false,
			name            = obj['name'] || false,
			description     = obj['description'] || false,
			gridLabel       = obj['gridLabel'] || false,
			copyright       = obj['copyright'] || '© ' + (new Date).getFullYear() + vendor
		;

		if(!mapTextureURL || !namespace || !vendor || !name || !description || !gridLabel){
			var
				missing = []
			;
			if(!mapTextureURL){
				missing.push('mapTextureURL');
			}
			if(!namespace){
				missing.push('namespace');
			}
			if(!vendor){
				missing.push('vendor');
			}
			if(!name){
				missing.push('name');
			}
			if(!description){
				missing.push('description');
			}
			if(!gridLabel){
				missing.push('gridLabel');
			}
			
			throw 'The following config properties were missing: ' + missing.join(', ');
		}else if(mapapi['gridConfigs'][namespace] != undefined){
			throw 'A grid config for that namespace has already been specified.';
		}

		for(var i in gridLookup){
			var
				region    = gridLookup[i],
				x         = region['x'],
				y         = region['y'],
				name      = region['Name'],
				uuid      = region['UUID'],
				width     = region['width'],
				height    = region['height'],
				regionObj = {
					'name'   : name,
					'uuid'   : uuid,
					'x'      : x,
					'y'      : y,
					'width'  : width,
					'height' : height
				},
				j         = Math['max'](width / 256, 1),
				k         = Math['max'](height / 256, 1)
			;
			for(var _x=x;_x<x + j;++_x){
				pos2region[_x]                  = pos2region[_x] || {};
				for(var _y=y;_y<y + k;++_y){
					pos2region[_x][_y]               = regionObj;
				}
			}
			region2pos[name.toLowerCase()] = regionObj;
		}
		
		var aurorasimTilesource = new tileSource({
			'copyright'       : copyright,
			'label'           : tileLabel,
			'minZoom'         : minZoom,
			'maxZoom'         : maxZoom,
			'backgroundColor' : backgroundColor
		});
		
		aurorasimTilesource['getTileURL'] = function(pos, zoom){
			if(!mapTextureURL){
				return null;
			}
			var
				zoom                  = Math.floor(zoom + 1),
				regions_per_tile_edge = Math.pow(2, zoom - 1),
				x                     = pos['x'],
				y                     = pos['y']
			;
			x -= x % regions_per_tile_edge;
			y -= y % regions_per_tile_edge; 

			for(var _x=x;_x<x+regions_per_tile_edge;++_x){
				for(var _y=y;_y<y+regions_per_tile_edge;++_y){
					if(pos2region[_x] && pos2region[_x][_y]){
						return mapTextureURL.replace('_%x%_', x).replace('_%y%_', y).replace('_%zoom%_', zoom - 1);
					}
				}
			}
			return null;
		}

		var
			aurorasim = new gridConfig({
				'namespace'   : namespace,
				'vendor'      : vendor,
				'name'        : name,
				'description' : description,
				'label'       : gridLabel,
				'size'        : size,
				'tileSources' : [
					aurorasimTilesource
				],
				'minZoom'     : minZoom,
				'maxZoom'     : maxZoom,
				'pos2region'  : function(pos, success, fail){
					if(!(pos instanceof mapapi['gridPoint'])){
						throw 'Position should be an instance of mapapi.gridPoint';
					}
					var
						x = Math.floor(pos['x']),
						y = Math.floor(pos['y'])
					;
					if(pos2region[x] && pos2region[x][y]){
						success({
							'pos'    : pos,
							'region' : pos2region[x][y]['name']
						});
					}else if(fail != undefined){
						fail('No region at coordinates (' + pos['x'] + ', ' + pos['y'] + ')');
					}
				},
				'region2pos' : function(region, success, fail){
					region = region.toLowerCase();
					if(region2pos[region] != undefined){
						var
							x    = region2pos[region]['x'],
							y    = region2pos[region]['y'],
							name = region2pos[region]['name']
						;
						success({
							'pos'    : gridPoint['fuzzy']({ 'x' : x, 'y' : y}),
							'region' : name
						});
					}else if(fail != undefined){
						fail('No region could be found with the specified name (' + region + ')');
					}
				}
			})
		;
		mapapi['gridConfigs'][namespace] = aurorasim;
		
		return aurorasim;
	}
})(window);

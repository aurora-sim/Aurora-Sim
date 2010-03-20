/*
 * Isomorphic SmartClient
 * Version 8.0 (2010-03-03)
 * Copyright(c) 1998 and beyond Isomorphic Software, Inc. All rights reserved.
 * "SmartClient" is a trademark of Isomorphic Software, Inc.
 *
 * licensing@smartclient.com
 *
 * http://smartclient.com/license
 */

if(window.isc&&window.isc.module_Core&&!window.isc.module_Forms){isc.module_Forms=1;isc._moduleStart=isc._Forms_start=(isc.timestamp?isc.timestamp():new Date().getTime());if(isc._moduleEnd&&(!isc.Log||(isc.Log && isc.Log.logIsDebugEnabled('loadTime')))){isc._pTM={ message:'Forms load/parse time: ' + (isc._moduleStart-isc._moduleEnd) + 'ms', category:'loadTime'};
if(isc.Log && isc.Log.logDebug)isc.Log.logDebug(isc._pTM.message,'loadTime')
else if(isc._preLog)isc._preLog[isc._preLog.length]=isc._pTM
else isc._preLog=[isc._pTM]}isc.A=isc.Canvas;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.applyTableResizePolicy=function(_1,_2,_3,_4,_5,_6,_7){var _8=this.logIsDebugEnabled("tablePolicy"),_9=this.logIsInfoEnabled("tablePolicy"),_10=this.logIsDebugEnabled("tablePlacement");var _11=_1.$8j;if(!this.$8k(_1)){_11=_1.$8j=[];var _12=0,_13=0;for(var _14=0;_14<_1.length;_14++){var _15=_1[_14];if(!_15.alwaysTakeSpace&&!_15.visible)continue;var _16=_15.getColSpan(),_17=_15.getRowSpan();if(_17==0||_16==0)continue;if(_16==null)_16=1;if(_17==null)_17=1;var _18=_16;if(_16=="*")_18=1;var _19=_15.getTitleOrientation();if(_15.showTitle&&(_19==isc.Canvas.LEFT||_19==isc.Canvas.RIGHT))
{_18+=1;if(_16!="*")_16+=1}
var _20=(_15.isStartRow?_15.isStartRow():_15.startRow),_21=(_15.isEndRow?_15.isEndRow():_15.endRow);if(_10){this.logDebug("at: "+["row"+_12,"col"+_13]+", item: "+(_15.name||_15.Class)+(_16=="*"?", colSpan:'*'":"")+", required cols:"+_18+(_17>1?", rowSpan:"+_17:"")+(_20?", startRow:true":"")+(_21?", endRow:true":""),"tablePlacement")}
var _22=null,_23=null;if(_13>=_4||(_20&&_13!=0)){_12++;_13=0;_15.$8l=true}else{_15.$8l=false}
if(_12<_11.length){for(;_12<_11.length;_12++){var _24=_11[_12];if(_24==null)break;for(;_13<_4;_13++){if(_24[_13]!=null)continue;for(var j=_13;j<_4;j++){if(_24[j]!=null)break;if((j-_13)+1>=_18){_22=_12;_23=_13;break}}
if(_23!=null)break}
if(_23!=null)break;_13=0;_15.$8l=true}}
if(_23==null){_22=_12;_23=0;_15.$8l=true}
_13=_23;if(_16=="*")_16=_4-_13;if(!isc.isA.Number(_17))_17=1;for(var r=_12;r<_12+_17;r++){if(!_11[r])_11[r]=[];for(var c=_13;c<_13+_16;c++){_11[r][c]=_14}}
_15.$8m=[_23,_22,_23+_16,_22+_17];_13+=_16;if(_21)_13=_4;if(_10){this.logDebug("item: "+(_15.name||_15.Class)+" placed at: "+["row"+_22,"col"+_23]+(_15.$8l?", marked startRow ":"")+", rowTable: "+this.echoAll(_11),"tablePlacement")}}
var _28=0;for(var r=0;r<_11.length;r++){var _29=_11[r];if(_29==null)break;var _30=0,_31=null;for(var c=0;c<_29.length;c++){if(_29[c]==null){_30++;continue}
if(r>0&&_11[r-1]!=null&&_29[c]==_11[r-1][c])continue;var _14=_29[c],_15=_1[_14];if(_15==_31||_15==null)continue;_15.$8n=_28;_15.$8o=_30;if(_10&&(_30>0||_28>0)){this.logDebug("itemNum:"+_14+" ("+(_15.name||_15.Class)+") at: "+["row"+_22,"col"+_23]+" preceded by "+(_30>0?_30+" empty cells":"")+(_28>0?" "+_28+" empty rows":""),"tablePlacement")}
_30=_28=0;_31=_15}
if(_31==null){_28++;_30=0}}}
if(!_5||!isc.isAn.Array(_5)){if(!isc.isAn.Array(_5)){this.logWarn(" 'colWidths' not an array - Ignoring.","tableResizePolicy")}
_5=[]}
_5=_5.duplicate();for(var c=0;c<_5.length;c++){var _32=_5[c];if(isc.isA.String(_32)){if(_32=="*")_5[c]=[0,10000,0,1];else if(_32.contains("*"))_5[c]=[0,10000,0,parseInt(_32)];else if(_32.contains("%"))_5[c]=[0,10000,parseInt(_32),0];else{var _33=parseInt(_32);if(_33==_32){_5[c]=_33}else{this.logWarn("Failed to understand specified colWidth:"+_32);_5[c]=[0,10000,0,1]}}}}
_1.colWidths=_5;if(!_6){_6=[];for(var r=0;r<_11.length;r++){var _29=_11[r],_34=null,_35=100000,_36=0,_37=0;if(!_29)continue;for(var c=0;c<_29.length;c++){var _15=_1[_29[c]];if(!_15)continue;var _38=_15.getCellHeight(_7);var _17=(_15.$8m[3]-_15.$8m[1]);if(_8)this.logWarn("item at: "+[r,c]+" has height: "+_38+", item is: "+_15);_15.$8p=false;if(isc.isA.Number(_38)){_38=Math.floor(_38/ _17);if(_8)this.logWarn("item: "+_15+" has pixel size: "+_38);if(_34==null||_38>_34){_34=_38}
if(_38>_35)_35=_38}else if(isc.isA.String(_38)){if(_38.contains("*")){_15.$8p=true;var _39=(_38=="*"?1:parseFloat(_38))
/ _17;                        if (_8) this.logWarn("item: " + _15 + " has star size: " + _39);						_37 = Math.max(_37, _39);					// else if height is a percentage
					} else {                        _15.$8p = true;						// get the percentage as a number
						// NOTE: if the item takes up more than one row, split it evenly across
                        // its rows
						var _40 = parseFloat(_38) /_17;if(_8)this.logWarn("item: "+_15+" has percent size: "+_40);if(_40>_36)_36=_40}
if(_15.minHeight>_34){_34=_15.minHeight}
if(_15.minHeight>_35){_35=_15.minHeight}
if(_15.maxHeight<_35&&_34<_15.maxHeight)
{_35=_15.maxHeight}}
if(_36>0||_37>0){if(_34==null)_34=0;_6[r]=[_34,_35,_36,_37]}else{if(_34==null){_34=_1.$8q||22}
_6[r]=_34}}}}
_1.rowHeights=_6;if(_9)this.logInfo("\ntotalWidth: "+_2+", totalHeight: "+_3+"\nspecified sizes:\n"+"cols:"+this.echoAll(_1.colWidths)+", rows: "+this.echoAll(_1.rowHeights),"tablePolicy");_1.$8r=_5=isc.Canvas.stretchResizeList(_1.colWidths,_2);_1.$8s=_6=isc.Canvas.stretchResizeList(_1.rowHeights,_3);if(_9)this.logInfo("\nderived sizes:\n"+"cols:"+this.echoAll(_1.$8r)+", rows: "+this.echoAll(_1.$8s),"tablePolicy");for(_14=0;_14<_1.length;_14++){_15=_1[_14];if(!_15.visible)continue;var _41=isc.isA.Canvas(_15),_32=_41?_15.getWidth():_15.width,_42=_41?_15.getHeight():_15.getCellHeight(_7),_19=_15.getTitleOrientation(),_43=_15.$8m,_44=0;if(_15.showTitle){if(_19==isc.Canvas.LEFT){_44=_5[_43[0]]}else{_44=_5[_43[2]]}}
if(_32=="*"){_32=0;var _45=(_15.showTitle&&_19==isc.Canvas.LEFT)?1:0,_46=(_15.showTitle&&_19==isc.Canvas.RIGHT)?1:0,_47=_43[0]+_45,_48=Math.min(_5.length,_43[2]-_46);for(var c=_47;c<_48;c++){_32+=_5[c]}}
if(_15.$8p){_42=0;var _20=_43[1],_21=_43[3];for(var c=_20;c<_21;c++){_42+=_6[c]}}
_15.$8t=[_32,_42];_15.$8u=_44}}
,isc.A.$8k=function(_1){if(!_1.$8j)return false;return true}
,isc.A.invalidateTableResizePolicy=function(_1){delete _1.$8j;delete _1.$8s;delete _1.$8r}
,isc.A.stretchResizeList=function(_1,_2){var _3=0,_4=0,_5=0,_6=_1.duplicate();for(var i=0;i<_1.length;i++){var _8=_6[i];if(isc.isA.Number(_8)){_8=Math.max(_8,1);_5+=_8;_6[i]=_8}else{var _9=_8[2],_10=_8[3];if(_10==0){_3+=_9}
_4+=_10}}
if(_4){var _11=0;if(_3<100){_11=(100-_3)/_4}
for(var r=0;r<_1.length;r++){var _8=_6[r];if(isc.isA.Number(_8))continue;var _9=_8[2],_10=_8[3],_13=_10*_11;if(_9<_13){_8[2]=_13}
if(_10>0)_3+=_8[2]}}
if(_3<=0)return _6;var _14=Math.max(0,_2-_5);for(var r=0;r<_1.length;r++){var _15=Math.max(0,_14/ _3),_8=_6[r];if(isc.isA.Number(_8))continue;var _16=_8[0];if(_16==0)continue;var _17=_8[2],_18=_15*_17;if(_18<_16){_6[r]=_16;_14-=_16;_3-=_17;r=0}}
for(var r=0;r<_1.length;r++){var _15=Math.max(0,_14/ _3),_8=_6[r];if(isc.isA.Number(_8))continue;var _19=_8[1],_17=_8[2],_18=_15*_17;if(_18>_19){_6[r]=_19;_14-=_19;_3-=_17;r=0}}
_15=Math.max(0,_14/ _3);for(var r=0;r<_1.length;r++){_8=_6[r];if(isc.isA.Number(_8))continue;var _17=_8[2];_6[r]=Math.floor(_17*_15)}
return _6}
);isc.B._maxIndex=isc.C+4;isc.ClassFactory.defineClass("ButtonTable",isc.Canvas);isc.A=isc.ButtonTable.getPrototype();isc.A.cellSpacing=0;isc.A.cellPadding=2;isc.A.cellBorder=0;isc.A.tableStyle="menuTable";isc.A.baseButtonStyle="button";isc.A.backgroundColor="CCCCCC";isc.A=isc.ButtonTable.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.getInnerHTML=function(){var _1=isc.SB.newInstance();_1.append("<TABLE"," CLASS=",this.tableStyle," WIDTH=",this.getWidth()-(this.overflow==isc.Canvas.SCROLL||this.overflow==isc.Canvas.AUTO?this.getScrollbarSize():0)," HEIGHT=",this.getHeight()," CELLSPACING=",this.cellSpacing," CELLPADDING=",this.cellPadding," BORDER=",this.cellBorder,"><TR>");for(var r=0;r<this.items.length;r++){var _3=this.items[r];_1.append("<TR>");if(!isc.isAn.Array(_3))_3=[_3];for(var i=0;i<_3.length;i++){var _5=_3[i];if(_5.action){_1.append(this.getCellButtonHTML(_5.contents,_5.action,_5.style,_5.disabled,_5.selected,_5.align,_5.extraTagStuff))}else{_1.append(this.getCellHTML(_5.contents,_5.style,_5.align,_5.extraTagStuff))}}
_1.append("</TR>")}
_1.append("</TABLE>");return _1.toString()}
,isc.A.showModal=function(){this.showClickMask(this.getID()+".hide()");this.show();this.unmask();this.bringToFront()}
,isc.A.hide=function(){this.Super("hide",arguments);this.hideClickMask();this.$8v=null}
,isc.A.getButtonBaseStyle=function(_1){var _2;if(_1)_2=_1.getAttribute("basestyle");if(!_2)_2=this.baseButtonStyle;return _2}
,isc.A.getMouseOutStyle=function(_1){var _2=this.getButtonBaseStyle(_1);if(this.buttonIsSelected(_1)){_2+="Selected"}
if(this.buttonIsDisabled(_1)){_2+="Disabled"}
return _2}
,isc.A.buttonIsSelected=function(_1){return _1&&_1.getAttribute("buttonselected")}
,isc.A.buttonIsDisabled=function(_1){return _1&&_1.getAttribute("buttondisabled")}
,isc.A.cellButtonOver=function(_1){var _2=this.getButtonBaseStyle(_1);if(this.buttonIsSelected(_1))_2+="Selected";if(_1)_1.className=_2+"Over"}
,isc.A.cellButtonOut=function(_1){if(!_1)return;_1.className=this.getMouseOutStyle(_1)}
,isc.A.cellButtonDown=function(_1){if(_1){var _2=this.getButtonBaseStyle(_1);if(this.buttonIsSelected(_1))_2+="Selected";_2+="Down"
_1.className=_2}}
,isc.A.getCellHTML=function(_1,_2,_3,_4){return isc.StringBuffer.concat("<TD ALIGN=",(_3||isc.Canvas.CENTER)," CLASS=",(_2||this.baseButtonStyle+"Disabled"),(_4||_4),">",_1,"</TD>")}
,isc.A.getCellButtonHTML=function(_1,_2,_3,_4,_5,_6,_7){if(_3==null)_3=this.baseButtonStyle;var _8=_3;if(_4)_8+="Selected";if(_5)_8+="Disabled";return isc.StringBuffer.concat("<TD ALIGN=",(_6||isc.Canvas.CENTER)," CLASS=",_8," ONMOUSEOVER='",this.getID(),".cellButtonOver(this);return false;' "," ONMOUSEOUT='",this.getID(),".cellButtonOut(this);return true;'"," ONMOUSEDOWN='",this.getID(),".cellButtonDown(this);return true;'"," ONMOUSEUP='",this.getID(),".cellButtonOut(this)';return true;"," basestyle='",_3,"'",(_4?" buttonselected='true'":null),(_5?" buttondisabled='true'":null),(_7?" "+_7:null)," ONCLICK=\""+_2+"\">",_1,"</TD>")}
);isc.B._maxIndex=isc.C+12;isc.ClassFactory.defineClass("DateChooser","ButtonTable");isc.A=isc.DateChooser.getPrototype();isc.A.defaultWidth=150;isc.A.defaultHeight=171;isc.A.showHeader=true;isc.A.headerHeight=20;isc.A.showYearButtons=true;isc.A.showYearChooser=true;isc.A.showMonthButtons=true;isc.A.showMonthChooser=true;isc.A.skinImgDir="images/common/";isc.A.prevYearIcon="[SKIN]doubleArrow_left.gif";isc.A.prevYearIconWidth=14;isc.A.prevYearIconHeight=7;isc.A.prevMonthIcon="[SKIN]arrow_left.gif";isc.A.prevMonthIconWidth=7;isc.A.prevMonthIconHeight=7;isc.A.nextYearIcon="[SKIN]doubleArrow_right.gif";isc.A.nextYearIconWidth=14;isc.A.nextYearIconHeight=7;isc.A.nextMonthIcon="[SKIN]arrow_right.gif";isc.A.nextMonthIconWidth=7;isc.A.nextMonthIconHeight=7;isc.A.showDoubleYearIcon=true;isc.A.yearMenuStyle="dateChooserYearMenu";isc.A.startYear=1990;isc.A.endYear=2010;isc.A.monthMenuStyle="dateChooserMonthMenu";isc.A.showTodayButton=true;isc.A.showCancelButton=false;isc.A.todayButtonTitle="Today";isc.A.cancelButtonTitle="Cancel";isc.A.disableWeekends=false;isc.A.showWeekends=true;isc.A.firstDayOfWeek=0;isc.A.year=new Date().getFullYear();isc.A.month=new Date().getMonth();isc.A.chosenDate=new Date();isc.A.baseButtonStyle="dateChooserButton";isc.A.alternateStyleSuffix="Dark";isc.A.headerStyle="dateChooserButtonDisabled";isc.A.useBackMask=true;isc.A.canFocus=true;isc.A=isc.DateChooser.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.show=function(){var _1=this.Super("show",arguments);if(this.autoClose){this.showClickMask(this.getID()+".close()",true);this.bringToFront()}}
,isc.A.setData=function(_1){if(!isc.isA.Date(_1))_1=new Date();this.year=_1.getFullYear();this.month=_1.getMonth();this.chosenDate=_1;this.markForRedraw()}
,isc.A.getData=function(){return this.chosenDate}
,isc.A.getInnerHTML=function(){if(!this.showHeader)this.headerHeight=0;this.baseWeekdayStyle=this.baseWeekdayStyle||this.baseButtonStyle;this.baseWeekendStyle=this.baseWeekendStyle||this.baseWeekdayStyle;var _1=this.baseNavButtonStyle||this.baseButtonStyle;var d=new Date(this.year,this.month,1),_3=new Date(this.year,this.month,1),_4=isc.SB.create();if(this.showHeader){_4.append("<TABLE WIDTH=100%"," HEIGHT=",(this.headerHeight+1)," CELLSPACING=",this.cellSpacing," CELLPADDING=",this.cellPadding," BORDER=",this.cellBorder,"><TR>");if(this.showYearButtons){var _5;if(this.showDoubleYearIcon){var _6=this.imgHTML(this.prevMonthIcon,this.prevMonthIconWidth,this.prevMonthIconHeight);_5=_8?"&nbsp;":"<NOBR>"+_6+_6+"<\/NOBR>"}else{_5=_8?"&nbsp;":this.imgHTML(this.prevYearIcon,this.prevYearIconWidth,this.prevYearIconHeight)}
_4.append(this.getCellButtonHTML(_5,this.getID()+".showPrevYear()",_1,null,null,isc.Canvas.CENTER," WIDTH=15"))}
if(this.showMonthButtons){_4.append(this.getCellButtonHTML(this.imgHTML(this.prevMonthIcon,this.prevMonthIconWidth,this.prevMonthIconHeight),this.getID()+".showPrevMonth()",_1,null,null,isc.Canvas.CENTER," WIDTH=15"))}
if(this.showMonthChooser){_4.append(this.getCellButtonHTML(_3.getShortMonthName(),this.getID()+".showMonthMenu()",_1,null,null,isc.Canvas.RIGHT," WIDTH=50%"))}else{_4.append(this.getCellHTML(_3.getShortMonthName(),_1+"Disabled"))}
if(this.showYearChooser){_4.append(this.getCellButtonHTML(_3.getFullYear(),this.getID()+".showYearMenu()",_1,null,null,isc.Canvas.LEFT," WIDTH=50%"))}else{_4.append(this.getCellHTML(_3.getShortFullName(),_1+"Disabled"))}
if(this.showMonthButtons){var _7=(_3.getFullYear()==9999&&_3.getMonth()==11);_4.append(this.getCellButtonHTML(_7?"&nbsp;":this.imgHTML(this.nextMonthIcon,this.nextMonthIconWidth,this.nextMonthIconHeight),_7?"":this.getID()+".showNextMonth()",_1,null,_7?true:null,isc.Canvas.CENTER," WIDTH=15"))}
if(this.showYearButtons){var _8=_3.getFullYear()==9999;var _9;if(this.showDoubleYearIcon){var _6=this.imgHTML(this.nextMonthIcon,this.nextMonthIconWidth,this.nextMonthIconHeight);_9=_8?"&nbsp;":"<NOBR>"+_6+_6+"<\/NOBR>"}else{_9=_8?"&nbsp;":this.imgHTML(this.nextYearIcon,this.nextYearIconWidth,this.nextYearIconHeight)}
_4.append(this.getCellButtonHTML(_9,_8?"":this.getID()+".showNextYear()",_1,null,_8?true:null,isc.Canvas.CENTER," WIDTH=15"))}
_4.append("<\/TR><\/TABLE>")}
_4.append("<TABLE WIDTH=100% HEIGHT=",(this.getHeight()-this.headerHeight)," CELLSPACING=0 CELLPADDING=2 BORDER=",this.cellBorder,">");_4.append("<TR HEIGHT=15>");var _10=this.getDayNames();var _11=Date.getWeekendDays();for(var i=0;i<_10.length;i++){var _13=_11.contains((i+this.firstDayOfWeek)%7)
if(_13&&!this.showWeekends)continue;var _14=(_13&&this.weekendHeaderStyle)?this.weekendHeaderStyle:this.headerStyle;_4.append(this.getCellHTML("<B>"+_10[(i+this.firstDayOfWeek)%7]+"</B>",_14))}
_4.append("<\/TR>");_3.setDate(_3.getDate()
-_3.getDay()+this.firstDayOfWeek
-((_3.getDay()<this.firstDayOfWeek)?7:0));var _15;var _16=false;while(true){if(this.alternateWeekStyles)_16=!_16;_4.append("<TR>");for(var i=0;i<7;i++){var _17=_3.getDay(),_18=_11.contains(_17);if(!(!this.showWeekends&&_18)){var _19=!_18?this.baseWeekdayStyle:this.baseWeekendStyle;if(_16)_19+=this.alternateStyleSuffix;_4.append(this.getDayCellButtonHTML((_15?null:_3),_19))}
if(this.year==9999&&this.month==11&&_3.getDate()==31){_15=true}else{var _20=_3.getDate();_3.setDate(_3.getDate()+1);if(_20==_3.getDate())_3.setDate(_3.getDate()+1)}}
_4.append("<\/TR>");if(_3.getMonth()!=this.month||_15)break}
if(this.showTodayButton||this.showCancelButton){_4.append("<TR");if(this.todayButtonHeight!=null)_4.append(" HEIGHT=",this.todayButtonHeight);_4.append(">");if(this.showTodayButton){var _21=!this.showCancelButton?(this.showWeekends?7:5):(!this.showWeekends?3:4);_4.append(this.getCellButtonHTML(this.todayButtonTitle,this.getID()+".todayClick()",this.baseBottomButtonStyle||this.baseButtonStyle,null,null,isc.Canvas.CENTER," COLSPAN="+_21))}
if(this.showCancelButton){var _21=!this.showTodayButton?(this.showWeekends?7:5):(!this.showWeekends?2:4);_4.append(this.getCellButtonHTML(this.cancelButtonTitle,this.getID()+".cancelClick()",this.baseBottomButtonStyle||this.baseButtonStyle,null,null,isc.Canvas.CENTER," COLSPAN="+_21))}
_4.append("<\/TR>")}
_4.append("<\/TABLE>");return _4.toString()}
,isc.A.getDayNames=function(){if(isc.DateChooser.$8w==null){var _1=new Date();isc.DateChooser.$8w=Date.getShortDayNames(2)}
return isc.DateChooser.$8w}
,isc.A.getDayCellButtonHTML=function(_1,_2,_3){if(_1==null)
return this.getCellButtonHTML("&nbsp;",null,_2,false,false,isc.Canvas.CENTER);var _4=(this.chosenDate&&(_1.toShortDate()==this.chosenDate.toShortDate())),_5=(_1.getMonth()!=this.month);var _6=this.getID()+".dateClick("+_1.getFullYear()+","+_1.getMonth()+","+_1.getDate()+");";if(this.disableWeekends&&Date.getWeekendDays().contains(_1.getDay())){_5=true;_6="return false;"}
return this.getCellButtonHTML(_1.getDate(),_6,_2,_4,_5,isc.Canvas.CENTER)}
,isc.A.dateIsSelected=function(_1){return null}
,isc.A.showPrevMonth=function(){if(--this.month==-1){this.month=11;this.year--}
this.markForRedraw()}
,isc.A.showNextMonth=function(){if(++this.month==12){this.month=0;this.year++}
this.markForRedraw()}
,isc.A.showMonth=function(_1){this.month=_1;if(this.monthMenu)this.monthMenu.hide();this.markForRedraw()}
,isc.A.showMonthMenu=function(){if(!this.monthMenu){var _1=[[]],_2=new Date(2001,0,1);for(var i=0;i<12;i++){_2.setMonth(i);_1[_1.length-1].add({contents:_2.getShortMonthName(),action:this.getID()+".showMonth("+i+")"});if((i+1)%3==0)_1.add([])}
this.monthMenu=isc.ButtonTable.newInstance({styleName:this.monthMenuStyle,left:this.getPageLeft()+5,top:this.getPageTop()+this.headerHeight,width:Math.min(this.getWidth(),120),height:Math.min(this.getHeight()-this.headerHeight,80),items:_1,visibility:isc.Canvas.HIDDEN,baseButtonStyle:this.baseButtonStyle});this.monthMenu.setPageLeft(this.getPageLeft()+((this.width-this.monthMenu.width)/2))}else{var _4=this.getPageTop()+this.headerHeight,_5=Math.min(this.getWidth(),120),_6=Math.min(this.getHeight()-this.headerHeight,80),_7=this.getPageLeft()+((this.width-_5)/2)
this.monthMenu.setPageRect(_7,_4,_5,_6)}
this.monthMenu.showModal()}
,isc.A.showPrevYear=function(){this.year--;this.markForRedraw()}
,isc.A.showNextYear=function(){this.year++;this.markForRedraw()}
,isc.A.showYear=function(_1){this.year=_1;if(this.yearMenu)this.yearMenu.hide();this.markForRedraw()}
,isc.A.showYearMenu=function(){if(!this.yearMenu){var _1=[[]];for(var i=0;i<=(this.endYear-this.startYear);i++){var _3=i+this.startYear;_1[_1.length-1].add({contents:_3,action:this.getID()+".showYear("+_3+")"});if((i+1)%3==0)_1.add([])}
this.yearMenu=isc.ButtonTable.newInstance({styleName:this.yearMenuStyle,top:this.getPageTop()+this.headerHeight,width:Math.min(this.getWidth(),120),height:Math.min(this.getHeight()-this.headerHeight,80),items:_1,visibility:isc.Canvas.HIDDEN,baseButtonStyle:this.baseButtonStyle});this.yearMenu.setPageLeft(this.getPageLeft()+((this.width-this.yearMenu.width)/2))}else{var _4=this.getPageTop()+this.headerHeight,_5=Math.min(this.getWidth(),120),_6=Math.min(this.getHeight()-this.headerHeight,80),_7=this.getPageLeft()+((this.width-_5)/2)
this.yearMenu.setPageRect(_7,_4,_5,_6)}
this.yearMenu.showModal()}
,isc.A.dateClick=function(_1,_2,_3){var _4=this.chosenDate=new Date(_1,_2,_3);this.month=_2;this.year=_1;this.dataChanged();if(window.dateClickCallback){if(isc.isA.String(window.dateClickCallback)){window.dateClickCallback=new Function("date",window.dateClickCallback)}
window.dateClickCallback(_4)}
if(this.autoHide)this.hide();if(this.autoClose)this.close();if(this.isDrawn())this.markForRedraw();return _4}
,isc.A.dataChanged=function(){}
,isc.A.cancelClick=function(){this.close()}
,isc.A.todayClick=function(){this.dateClick(new Date().getFullYear(),new Date().getMonth(),new Date().getDate())}
,isc.A.close=function(){this.hideClickMask();if(this.yearMenu&&this.yearMenu.isVisible())this.yearMenu.hide();if(this.monthMenu&&this.monthMenu.isVisible())this.monthMenu.hide();if(this.isDrawn())this.clear()}
);isc.B._maxIndex=isc.C+20;isc.A=isc.DateChooser;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.getSharedDateChooser=function(_1){if(!this.$8x){this.$8x=this.create(_1,{_generated:true,autoHide:true,showCancelButton:true});return this.$8x}
isc.addProperties(this.$8x,_1);return this.$8x}
);isc.B._maxIndex=isc.C+1;isc.ClassFactory.defineClass("Slider",isc.Canvas);isc.A=isc.Slider;isc.A.VERTICAL_SRC_PREFIX="v";isc.A.HORIZONTAL_SRC_PREFIX="h";isc.A.DOWN="down";isc.A.UP="";isc.A.EVENTNAME="sliderMove";isc.A=isc.Slider.getPrototype();isc.A.title="Set Value";isc.A.length=200;isc.A.vertical=true;isc.A.thumbThickWidth=23;isc.A.thumbThinWidth=17;isc.A.trackWidth=7;isc.A.skinImgDir="images/Slider/";isc.A.thumbSrc="thumb.gif";isc.A.trackSrc="track.gif";isc.A.trackCapSize=6;isc.A.trackImageType=isc.Img.STRETCH;isc.A.showTitle=true;isc.A.showRange=true;isc.A.showValue=true;isc.A.labelWidth=50;isc.A.labelHeight=20;isc.A.labelSpacing=5;isc.A.titleStyle="sliderTitle";isc.A.rangeStyle="sliderRange";isc.A.valueStyle="sliderValue";isc.A.value=1;isc.A.minValue=1;isc.A.maxValue=100;isc.A.roundValues=true;isc.A.roundPrecision=1;isc.A.flipValues=false;isc.A.canFocus=true;isc.A.stepPercent=5;isc.A.animateThumbTime=250;isc.A.animateThumbAcceleration="slowStartandEnd";isc.A.valueChangedOnDrag=true;isc.A.valueChangedOnRelease=true;isc.A.valueChangedOnClick=true;isc.A=isc.Slider.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.hValueLabelWidth=5;isc.B.push(isc.A.initWidget=function(){this.Super("initWidget",arguments);if(!(this.minValue<=this.maxValue)){this.logWarn("Slider specified with minValue:"+this.minValue+", greater than maxValue:"+this.maxValue+" - reversing max and min value.");var _1=this.minValue;this.minValue=this.maxValue;this.maxValue=_1}
this.setUpSize();this.$80();if(this.showTitle)this._titleLabel=this.addChild(this.$54());if(this.showRange){this.$50=this.addChild(this.$51("min"));this.$52=this.addChild(this.$51("max"))}
if(this.showValue){this._valueLabel=this._thumb.addPeer(this.$53());this._valueLabel.sendToBack();this.$81()}
this.setValue(this.value,!(this.animateThumbInit==true))}
,isc.A.setUpSize=function(){var _1=this.$pn,_2=this.$po;if(this.vertical){if(_1==null){var _3=Math.max(this.thumbThickWidth,this.trackWidth);if(this.showValue)_3+=this.labelWidth+this.labelSpacing;if(this.showRange)_3+=this.labelWidth+this.labelSpacing;this.logInfo("defaulting width to "+_3+"px");this.setWidth(_3)}
if(_2==null){var _4=this.length;if(this.showTitle)_4+=this.labelHeight+this.labelSpacing;if(this.showValue&&(this.labelHeight>this.thumbThinWidth)){_4+=(this.labelHeight-this.thumbThinWidth)}
this.logInfo("no specified height on vertical Slider - defaulting to:"+_4+" based on slider.length of "+this.length);this.setHeight(_4)}else{this.length=this.getHeight();if(this.showTitle)this.length-=(this.labelHeight+this.labelSpacing);if(this.showValue&&(this.labelHeight>this.thumbThinWidth)){this.length-=(this.labelHeight-this.thumbThinWidth)}
this.logInfo("setting slider track length to:"+this.length+", based on specified height")}}else{if(_2==null){var _4=Math.max(this.thumbThickWidth,this.trackWidth);if(this.showValue)_4+=this.labelHeight+this.labelSpacing;if(this.showRange)_4+=this.labelHeight+this.labelSpacing;this.logInfo("defaulting height to "+_4+"px");this.setHeight(_4)}
if(_1==null){var _3=(this.length+(this.showTitle?this.labelWidth+this.labelSpacing:0));if(this.showValue&&(this.labelWidth>this.thumbThinWidth)){_3+=(this.labelWidth-this.thumbThinWidth)}
this.logInfo("no specified width on horizontal Slider - defaulting to:"+_3+" based on slider.length of "+this.length);this.setWidth(_3)}else{this.length=this.getWidth();if(this.showTitle)this.length-=(this.labelWidth+this.labelSpacing);if(this.showValue&&(this.hValueLabelWidth>this.thumbThinWidth)){this.length-=(this.hValueLabelWidth-this.thumbThinWidth)}
this.logInfo("setting slider track length to:"+this.length+", based on specified width")}}
this.$8y=this.length-this.thumbThinWidth;if(this.numValues&&this.numValues>1){this.$8z=this.$8y/(this.numValues-1)}}
,isc.A.resizeBy=function(_1,_2){this.Super("resizeBy",arguments);if(!this._track)return;var _3=this.vertical;if((_3&&_2!=0)||(!_3&&_1!=0)){this.length+=_3?_2:_1;this.$8y=this.length-this.thumbThinWidth;if(_3)this._track.resizeBy(0,_2)
else this._track.resizeBy(_1,0);if(this.numValues&&this.numValues>1){this.$8z=this.$8y/(this.numValues-1)}
this.setValue(this.value,true);if(this.showRange){if(this.vertical){var _4=this.flipValues?this.$52:this.$50;_4.moveBy(0,_2)}else{var _4=this.flipValues?this.$50:this.$52;_4.moveBy(_1,0)}}}}
,isc.A.$51=function(_1){var _2,_3,_4,_5,_6=(this.vertical?_1=="max":_1=="min");if(this.flipValues)_6=!_6;if(this.vertical){_2=Math.max(this.thumbThickWidth,this.trackWidth)+this.labelSpacing+(this.showValue?this.labelWidth+this.labelSpacing:0);_4=isc.Canvas.LEFT;if(_6){_3=(this.showTitle?this.labelHeight+this.labelSpacing:0);_5=isc.Canvas.TOP}else{_3=(this.showTitle?this.labelHeight+this.labelSpacing:0)+(this.length-this.labelHeight);_5=isc.Canvas.BOTTOM}}else{_3=Math.max(this.thumbThickWidth,this.trackWidth)+this.labelSpacing+(this.showValue?this.labelHeight+this.labelSpacing:0);_5=isc.Canvas.TOP;if(_6){_2=(this.showTitle?this.labelWidth+this.labelSpacing:0);_4=isc.Canvas.LEFT}else{_2=(this.showTitle?this.labelWidth+this.labelSpacing:0)+this.length-this.labelWidth;_4=isc.Canvas.RIGHT}}
return isc.Label.create({ID:this.getID()+"_"+_1+"Label",autoDraw:false,left:_2,top:_3,width:this.labelWidth,height:this.labelHeight,wrap:false,align:_4,valign:_5,className:this.rangeStyle,contents:(_1=="min"?(this.minValueLabel?this.minValueLabel:this.minValue):(this.maxValueLabel?this.maxValueLabel:this.maxValue))})}
,isc.A.$54=function(){var _1=(this.vertical?isc.Canvas.CENTER:isc.Canvas.RIGHT);return isc.Label.create({ID:this.getID()+"_titleLabel",autoDraw:false,left:0,top:0,width:(this.vertical?this.getWidth():this.labelWidth),height:(this.vertical?this.labelHeight:this.getHeight()),align:_1,className:this.titleStyle,contents:this.title})}
,isc.A.$53=function(){var _1,_2,_3,_4,_5;if(this.vertical){_1=this._thumb.getLeft()-this.labelWidth-this.labelSpacing;_2=this._thumb.getTop()+parseInt(this._thumb.getHeight()/2-this.labelHeight/ 2);_4=isc.Canvas.RIGHT;_5=isc.Canvas.CENTER;_3=this.labelWidth}else{_1=this._thumb.getLeft()+parseInt(this._thumb.getWidth()/2-this.labelWidth/ 2);_2=this._thumb.getTop()-this.labelHeight-this.labelSpacing;_4=isc.Canvas.CENTER;_5=isc.Canvas.BOTTOM;_3=this.hValueLabelWidth}
var _6=isc.Label.create({ID:this.getID()+"_valueLabel",autoDraw:false,left:_1,top:_2,width:_3,height:this.labelHeight,wrap:false,align:_4,className:this.valueStyle,contents:this.value,mouseUp:function(){return false},moveWithMaster:false,observes:[{source:this,message:"valueChanged",action:"this.$81();"}]});if(!this.vertical){isc.addMethods(_6,{draw:function(){var _7=this.visibility
this.hide();this.Super("draw",arguments);this.parentElement.$81();this.setVisibility(this.prevVis)}})};return _6}
,isc.A.$80=function(){var _1=this.$82(),_2,_3,_4=(this.vertical?this.trackWidth:this.length),_5=(this.vertical?this.length:this.trackWidth),_6,_7,_8=(this.vertical?this.thumbThickWidth:this.thumbThinWidth),_9=(this.vertical?this.thumbThinWidth:this.thumbThickWidth);var _10=this.thumbThickWidth>this.trackWidth;if(_10){if(this.vertical){_6=_1[0];_2=_6+parseInt(this.thumbThickWidth/ 2-this.trackWidth/ 2);_3=_1[1];_7=_1[1]}else{_7=_1[1];_3=_7+parseInt(this.thumbThickWidth/ 2-this.trackWidth/ 2);_2=_1[0];_6=_1[0]}}else{if(this.vertical){_2=_1[0];_6=_2+parseInt(this.trackWidth/ 2-this.thumbThinWidth/ 2);_3=_1[1];_7=_1[1]}else{_3=_1[1];_7=_3+parseInt(this.trackWidth/ 2-this.thumbThinWidth/ 2);_2=_1[0];_6=_1[0]}}
this.logDebug("calculated coords for track:"+[_2,_3,_4,_5]);this.logDebug("calculated coords for thumb:"+[_6,_7,_8,_9]);this._track=this.addChild(this.$83(_3,_2,_4,_5));this._thumb=this._track.addPeer(this.$84(_7,_6,_8,_9))}
,isc.A.$82=function(){var _1=this.vertical?(this.showValue?this.labelWidth+this.labelSpacing:0):(this.showTitle?this.labelWidth+this.labelSpacing:0),_2=this.vertical?(this.showTitle?this.labelHeight+this.labelSpacing:0):(this.showValue?this.labelHeight+this.labelSpacing:0);if(this.showValue){if(this.vertical&&(this.labelHeight>this.thumbThinWidth))
_2+=Math.round((this.labelHeight-this.thumbThinWidth)/2);if(this.horizontal&&(this.labelWidth>this.thumbThinWidth))
_1+=Math.round((this.labelWidth-this.thumbThinWidth)/2)}
return[_1,_2]}
,isc.A.$83=function(_1,_2,_3,_4){return isc.StretchImg.create({ID:this.getID()+"_track",autoDraw:false,left:_2,top:_1,width:_3,height:_4,vertical:this.vertical,capSize:this.trackCapSize,src:"[SKIN]"+(this.vertical?isc.Slider.VERTICAL_SRC_PREFIX:isc.Slider.HORIZONTAL_SRC_PREFIX)+this.trackSrc,skinImgDir:this.skinImgDir,imageType:this.trackImageType,canFocus:true,tabIndex:-1,cacheImageSizes:false})}
,isc.A.$84=function(_1,_2,_3,_4){return isc.Img.create({ID:this.getID()+"_thumb",autoDraw:false,left:_2,top:_1,width:_3,height:_4,src:"[SKIN]"+(this.vertical?isc.Slider.VERTICAL_SRC_PREFIX:isc.Slider.HORIZONTAL_SRC_PREFIX)+this.thumbSrc,skinImgDir:this.skinImgDir,canDrag:true,dragAppearance:isc.EventHandler.NONE,cursor:isc.Canvas.HAND,dragMove:function(){this.parentElement.$85();return false},$jo:false,dragStart:function(){var _5=isc.EventHandler;_5.dragOffsetX=-1*(this.getPageLeft()-_5.mouseDownEvent.x);_5.dragOffsetY=-1*(this.getPageTop()-_5.mouseDownEvent.y);this.parentElement.$86=true;return _5.STOP_BUBBLING},dragStop:function(){this.parentElement.$86=false;this.setState(isc.Slider.UP);if(this.parentElement.valueChangedOnRelease){this.parentElement.valueChanged(this.parentElement.value)}
return false},mouseDown:function(){this.setState(isc.Slider.DOWN)},mouseUp:function(){this.setState(isc.Slider.UP);return false},canFocus:true,tabIndex:-1})}
,isc.A.$85=function(_1){var _2,_3;if(this.vertical){var _4=this._track.getTop(),_5=this.$8y+_4;_2=isc.EventHandler.getY()-isc.EventHandler.dragOffsetY-this.getPageTop();_2=Math.max(_4,Math.min(_5,_2));var _6=_2-_4;if(this.numValues){_6=Math.round(_6/ this.$8z)*this.$8z;_2=Math.round(_6)+_4}
if(_2==this._thumb.getTop())return;this.logDebug("drag-moving thumb to:"+_2)
if(_1&&this.animateThumb){this.$87=this._thumb.animateMove(this._thumb.getLeft(),_2,null,this.animateThumbTime,this.animateThumbAcceleration)}else{this._thumb.setTop(_2)}
_3=(this.flipValues?_6/ this.$8y:1-_6/ this.$8y)}else{var _7=this._track.getLeft(),_5=this.$8y+_7;_2=isc.EventHandler.getX()-isc.EventHandler.dragOffsetX-this.getPageLeft();_2=Math.max(_7,Math.min(_5,_2));var _6=_2-_7;if(this.numValues){_6=Math.round(_6/ this.$8z)*this.$8z;_2=Math.round(_6)+_7}
if(_2==this._thumb.getLeft())return;this.logDebug("drag-moving thumb to:"+_2)
if(_1&&this.animateThumb){this.$87=this._thumb.animateMove(_2,this._thumb.getTop(),null,this.animateThumbTime,this.animateThumbAcceleration)}else{this._thumb.setLeft(_2)}
_3=(this.flipValues?1-_6/ this.$8y:_6/ this.$8y)}
if(this.maxValue==this.minValue){this.value=this.minValue}else{var _8=_3*(this.maxValue-this.minValue)+this.minValue
if(this.roundValues)_8=Math.round(_8);else if(this.roundPrecision!=null){var _9=Math.pow(10,this.roundPrecision);_8=(Math.round(_8*_9))/_9}
this.value=_8}
this.logDebug("slider value from drag-move:"+this.value);if(this.valueChangedOnDrag||!this.$86){this.valueChanged(this.value)}
if(this.sliderTarget)isc.EventHandler.handleEvent(this.sliderTarget,isc.Slider.EVENTNAME,this)}
,isc.A.$81=function(){var _1=this._valueLabel;if(_1==null)return;_1.setContents(this.getValue());var _2=this._thumb;if(this.vertical){_1.setTop(parseInt((_2.getTop()+_2.getHeight()/2)-_1.getHeight()/2))}else{if(_1.isDrawn())_1.redraw();var _3=_1.getVisibleWidth(),_4=parseInt((_2.getLeft()+_2.getWidth()/2)-_3/ 2);if(_4+_3>this.getWidth()){_4=this.getWidth()-_3}
if(_4<0)_4=0;_1.setLeft(_4)}}
,isc.A.mouseUp=function(){isc.EventHandler.dragOffsetX=isc.EventHandler.dragOffsetY=Math.floor(this.thumbThinWidth/ 2);if(this.valueChangedOnClick)this.$85(true)}
,isc.A.setValue=function(_1,_2){var _3,_4;if(!isc.isA.Number(_1))return;_1=Math.max(this.minValue,(Math.min(_1,this.maxValue)));this.value=(this.roundValues?Math.round(_1):_1);if(this.minValue==this.maxValue)_3=1;else _3=(this.value-this.minValue)/(this.maxValue-this.minValue);_4=_3*this.$8y;var _5;if(this.vertical){_5=this._track.getTop()+parseInt(this.flipValues?_4:this.$8y-_4);if(this.animateThumb&&!_2){this.$87=this._thumb.animateMove(this._thumb.getLeft(),_5,null,this.animateThumbTime,this.animateThumbAcceleration)}else{this._thumb.setTop(_5)}}else{_5=this._track.getLeft()+parseInt(this.flipValues?this.$8y-_4:_4);if(this.animateThumb&&!_2){this.$87=this._thumb.animateMove(_5,this._thumb.getTop(),null,this.animateThumbTime,this.animateThumbAcceleration)}else{this._thumb.setLeft(_5)}}
this.valueChanged(this.value);if(this.sliderTarget)isc.EventHandler.handleEvent(this.sliderTarget,isc.Slider.EVENTNAME,this)}
,isc.A.getValue=function(){return this.value}
,isc.A.valueChanged=function(_1){}
,isc.A.valueIsChanging=function(){return(this.$86==true)}
,isc.A.handleKeyPress=function(_1,_2){var _3=_1.keyName;if(_3=="Home"){this.setValue(this.minValue,true);return false}
if(_3=="End"){this.setValue(this.maxValue,true);return false}
var _4=(this.maxValue-this.minValue)*this.stepPercent/ 100;if(this.roundValues&&_4<1)_4=1;if(this.vertical){if((this.flipValues&&_3=="Arrow_Up")||(!this.flipValues&&_3=="Arrow_Down"))
{this.setValue(this.getValue()-_4,true);return false}else if((this.flipValues&&_3=="Arrow_Down")||(!this.flipValues&&_3=="Arrow_Up"))
{this.setValue(this.getValue()+_4,true);return false}}else{if((this.flipValues&&_3=="Arrow_Left")||(!this.flipValues&&_3=="Arrow_Right"))
{this.setValue(this.getValue()+_4,true)
return false}else if((this.flipValues&&_3=="Arrow_Right")||(!this.flipValues&&_3=="Arrow_Left"))
{this.setValue(this.getValue()-_4,true)
return false}}
if(this.keyPress){this.convertToMethod("keyPress");return this.keyPress(_1,_2)}}
,isc.A.setCanFocus=function(_1){this.Super("canFocus",arguments);if(this._thumb!=null)this._thumb.setCanFocus(_1);if(this._track!=null)this._track.setCanFocus(_1)}
,isc.A.setMinValue=function(_1){this.minValue=_1;if(this.$50)this.$50.setContents(_1);this.setValue(this.minValue)}
,isc.A.setMaxValue=function(_1){this.maxValue=_1;if(this.$52)this.$52.setContents(_1);this.setValue(this.minValue)}
,isc.A.setNumValues=function(_1){this.numValues=_1;this.$8z=this.$8y/(this.numValues-1);this.setValue(this.minValue)}
,isc.A.setTitle=function(_1){this._titleLabel.setContents(_1)}
,isc.A.setLength=function(_1){this.length=_1;this.setUpSize()}
,isc.A.$74i=function(){this._titleLabel.destroy();this._track.destroy();this._thumb.destroy();this._valueLabel.destroy();this.$50.destroy();this.$52.destroy();this.initWidget()}
,isc.A.setVertical=function(_1){this.vertical=_1;this.$74i()}
,isc.A.setThumbThickWidth=function(_1){this.thumbThickWidth=_1;this.$74i()}
,isc.A.setThumbThinWidth=function(_1){this.thumbThinWidth=_1;this.$74i()}
,isc.A.setTrackWidth=function(_1){this.trackWidth=_1;this.$74i()}
,isc.A.setThumbSrc=function(_1){this.thumbSrc=_1;this.$74i()}
,isc.A.setTrackSrc=function(_1){this.trackSrc=_1;this.$74i()}
,isc.A.setTrackCapSize=function(_1){this.trackCapSize=_1;this.$74i()}
,isc.A.setTrackImageType=function(_1){this.trackImageType=_1;this.$74i()}
,isc.A.setShowTitle=function(_1){this.showTitle=_1;this.$74i()}
,isc.A.setShowRange=function(_1){this.showRange=_1;this.$74i()}
,isc.A.setShowValue=function(_1){this.showValue=_1;this.$74i()}
,isc.A.setLabelWidth=function(_1){this.labelWidth=_1;this.$74i()}
,isc.A.setLabelHeight=function(_1){this.labelHeight=_1;this.$74i()}
,isc.A.setLabelSpacing=function(_1){this.labelSpacing=_1;this.$74i()}
,isc.A.setMaxValueLabel=function(_1){this.$52.setContents(_1)}
,isc.A.setRoundValues=function(_1){this.roundValues=_1;this.$74i()}
,isc.A.setRoundPrecision=function(_1){this.roundPrecision=_1;this.$74i()}
,isc.A.setFlipValues=function(_1){this.flipValues=_1;this.$74i()}
,isc.A.setStepPercent=function(_1){this.stepPercent=_1;this.$74i()}
);isc.B._maxIndex=isc.C+44;isc.Slider.registerStringMethods({valueChanged:"value"})
if(isc.ListGrid){isc.ClassFactory.defineClass("ScrollingMenu","ListGrid");isc.A=isc.ScrollingMenu.getPrototype();isc.A.useBackMask=true;isc.A.canFocus=true;isc.A.showHeader=false;isc.A.showEdges=false;isc.A.autoDraw=false;isc.A.className="scrollingMenu";isc.A.bodyStyleName="scrollingMenuBody";isc.A.selectionType="single";isc.A.showRollOver=false;isc.A.leaveScrollbarGap=false;isc.A.generateClickOnSpace=false;isc.A.generateDoubleClickOnEnter=false;isc.A.generateClickOnEnter=true;isc.A.showModal=true;isc.A.arrowKeyAction="focus";isc.A=isc.ScrollingMenu.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.show=function(){if(this.showModal)this.showClickMask({target:this,methodName:"cancel"},false,[this]);this.Super("show",arguments);if(this.showModal)this.body.focus()}
,isc.A.recordClick=function(_1,_2,_3,_4,_5,_6,_7){this.hide();if(_2!=null)this.itemClick(_2)}
,isc.A.itemClick=function(_1){}
,isc.A.rowOver=function(_1,_2,_3){this.selection.selectOnRowOver(_1)}
,isc.A.createSelectionModel=function(_1,_2,_3,_4,_5){var _6=this.invokeSuper("ScrollingMenu","createSelectionModel",_1,_2,_3,_4,_5);this.selection.addProperties({selectOnRowOver:function(_7){this.selectSingle(_7);this.selectionFromMouse=true},setSelected:function(_7,_8){this.selectionFromMouse=false;return this.Super("setSelected",arguments)}});return _6}
,isc.A.$88=function(_1){this.Super("$88",arguments);this.selection.selectSingle(this.getRecord(_1))}
,isc.A.bodyKeyPress=function(_1,_2){var _3=_1.keyName;if(_3==this.$10j){var _4=this.selection;if(_4&&_4.selectionFromMouse){this.cancel();return false}}
if(_3=="Escape"){this.cancel();return false}
return this.Super("bodyKeyPress",arguments)}
,isc.A.cancel=function(){this.hide()}
,isc.A.hide=function(){this.hideClickMask();return this.Super("hide",arguments)}
,isc.A.dataChanged=function(){var _1=this.Super("dataChanged",arguments);if(this.data&&this.data.getLength()>0&&this.selection&&!this.selection.anySelected()&&(isc.isA.ResultSet==null||!isc.isA.ResultSet(this.data)||this.data.rowIsLoaded(0)))
{this.selection.selectItem(0)}
return _1}
);isc.B._maxIndex=isc.C+10}
isc.ClassFactory.defineClass("DynamicForm","Canvas","DataBoundComponent");isc.addGlobal("FormLayout",isc.DynamicForm);isc.A=isc.DynamicForm;isc.A.GET="GET";isc.A.POST="POST";isc.A.NORMAL="normal";isc.A.MULTIPART="multipart";isc.A.NORMAL_ENCODING="application/x-www-form-urlencoded";isc.A.MULTIPART_ENCODING="multipart/form-data";isc.A.$89="$89";isc.A.$9a="$9a";isc.A.$9b="$9b";isc.A.$9c="$9d";isc.A.$9e="$9f";isc.A.$9g="$9g";isc.A=isc.DynamicForm.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.flattenItems=false;isc.A.numCols=2;isc.A.fixedColWidths=false;isc.A.fixedRowHeights=false;isc.A.colWidths=null;isc.A.minColWidth=20;isc.A.cellSpacing=0;isc.A.cellPadding=2;isc.A.cellBorder=0;isc.A.defaultRowHeight=22;isc.A.sectionVisibilityMode="multiple";isc.A.allowContentAndChildren=true;isc.A.separateContentInsertion=true;isc.A.$r9=true;isc.A.fieldIdProperty="name";isc.A.titleField="title";isc.A.showDetailFields=true;isc.A.longTextEditorThreshold=255;isc.A.longTextEditorType="textArea";isc.A.titlePrefix="";isc.A.rightTitlePrefix=":&nbsp;";isc.A.titleSuffix="&nbsp;:";isc.A.rightTitleSuffix="";isc.A.titleWidth=100;isc.A.showInlineErrors=true;isc.A.showErrorIcons=true;isc.A.showErrorText=false;isc.A.showErrorStyle=true;isc.A.errorOrientation="left";isc.A.errorItemDefaults={type:"blurb",wrap:true,showIf:function(){return!this.form.showInlineErrors&&this.form.hasErrors()},defaultDynamicValue:function(_1,_2,_3){return _2.getErrorsHTML(_2.getErrors())}};isc.A.errorItemCellStyle="formCellError";isc.A.errorsPreamble="The following errors were found.";isc.A.hiliteRequiredFields=true;isc.A.requiredTitlePrefix="<B>";isc.A.requiredRightTitlePrefix="<B>:&nbsp;";isc.A.requiredTitleSuffix="&nbsp;:</B>";isc.A.requiredRightTitleSuffix="</B>";isc.A.canHover=false;isc.A.itemHoverDelay=500;isc.A.itemHoverStyle="formHover";isc.A.overflow=isc.Canvas.VISIBLE;isc.A.defaultHeight=20;isc.A.validateOnChange=false;isc.A.unknownErrorMessage="Invalid value";isc.A.synchronousValidation=false;isc.A.autoFocus=false;isc.A.selectOnFocus=false;isc.A.canFocus=true;isc.A._useNativeTabIndex=false;isc.A.$kn=false;isc.A.uniqueMatch=true;isc.A.browserSpellCheck=true;isc.A.cancelParamName="org.apache.struts.taglib.html.CANCEL";isc.A.cancelParamValue="cancel";isc.A.action="#";isc.A.method=isc.DynamicForm.POST;isc.A.encoding=isc.DynamicForm.NORMAL_ENCODING;isc.A.writeFormTag=true;isc.A.autoSendTargetFieldName="__target__";isc.A.useNativeSelectItems=false;isc.A.hideUsingDisplayNone=isc.Browser.isMoz&&isc.Browser.isMac;isc.A.operator="and";isc.A.nestedEditorType="NestedEditorItem";isc.A.nestedListEditorType="NestedListEditorItem";isc.A.canDropItems=false;isc.A.canAddColumns=true;isc.B.push(isc.A.hasInherentHeight=function(){if(this.inherentHeight!=null)return this.inherentHeight;return(this.overflow==isc.Canvas.VISIBLE||this.overflow==isc.Canvas.CLIP_H)}
);isc.B._maxIndex=isc.C+1;isc.A=isc.DynamicForm.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$9i="upload";isc.A.$9j="mutex";isc.A.$9k=["name","editorType","type","valueMap","defaultValue","showTitle","left","top","width","height"];isc.A.dataArity="single";isc.A.$70l="/";isc.A.$9l="form";isc.A.$9m="$9n";isc.A.$9o="$9p";isc.A.$9q="absolute";isc.A.$9r="showIf";isc.A.$9s="item,value,form,values";isc.A.$9t="</FORM>";isc.A.$9u="tablePolicy";isc.A.$9v="<COL WIDTH=";isc.A.$9w=(isc.Browser.isIE?"<TR STYLE='position:absolute'>":"<TR>");isc.A.$9x=["<TD style='",isc.Canvas.$42a,"height:0px;overflow:hidden;padding:0px;' class='",null,"'>",(isc.Browser.isSafari?"<div style='overflow:hidden;height:0px'>":"")];isc.A.$9y=(isc.Browser.isSafari?"</div></TD>":"</TD>");isc.A.$5u="<TD>";isc.A.$5v="</TD>";isc.A.$5s="<TR>";isc.A.$5t="</TR>";isc.A.$9z="<br>";isc.A.$90="</TABLE></FORM>";isc.A.$91=["<INPUT TYPE=HIDDEN NAME='",,"' VALUE='",,"'>"];isc.A.titleHeight=15;isc.A.$92="height:";isc.A.$93="width:";isc.A.$11j="max-width:";isc.A.$94="max-height:";isc.A.$92="height:";isc.A.$95="<NOBR>";isc.A.$96="</td></tr></TABLE>";isc.A.$97="</DIV>";isc.A.$98="</TD>";isc.A.$99=["<TD ",," CLASS='",,"' ALIGN='",,"' VALIGN='",,"'",,">"];isc.A.$10a=["<DIV style='overflow:hidden;text-overflow:ellipsis;",,,,"'>"];isc.A.$10b=["<TABLE height=",," border=0 cellspacing=0 cellpadding=0><tr><td class='",,"' style='"+isc.Canvas.$42a+"' ALIGN='",,"'>",null];isc.A.$63z="$428";isc.A.$10c=["<FORM ","ID","=",,," METHOD=",," ACTION='",,"' ENCTYPE=",,,,," ONSUBMIT='return ",,".$10d()' ONRESET='",,".resetValues(); return false;'",(isc.Browser.isSafari?" STYLE='display:inline;'":null)," STYLE='margin-bottom:0px;'>"];isc.A.$10e=" STYLE='position:absolute;left:0px;top:0px;'";isc.A.$10f=" TARGET='";isc.A.$10g=["<TABLE ID='",,"' ","WIDTH=",," CELLSPACING=",," CELLPADDING=",," BORDER=",,(isc.Browser.isMoz?"><TBODY>":">")];isc.A.$10h="table";isc.A.formSubmitFailedWarning="Form was unable to be submitted. The most likely cause for this is an "+"invalid value in an upload field.";isc.A.$10i="requiredIf";isc.A.$10j="Enter";isc.B.push(isc.A.initWidget=function(){if(isc.$cv)arguments.$cw=this;this.Super("initWidget",arguments);if(this.showComplexFieldsRecursively)this.showComplexFields=true;if(this.fields&&this.items==null)this.items=this.fields;if(this.defaultItems!=null&&this.items==null){this.items=[];for(var i=0;i<this.defaultItems.length;i++){this.items[i]=isc.addProperties({},this.defaultItems[i])}}
if(this.values==null)this.values={};if(this.action!=isc.DynamicForm.getPrototype().action&&this.action!=null&&!isc.isA.emptyString(this.action))
{this.setAction(this.action)}
this.setItems(this.items?this.items:[],true);if(this.isDisabled()){this.setDisabled(true)}
this.setErrors(this.errors?this.errors:{});this.setValues(this.values,true);if(this.selectionComponent!=null)this.setSelectionComponent(this.selectionComponent,true)}
,isc.A.$10k=function(_1){if(!_1)return;if(!isc.isA.FormItem(_1[0]))return;_1.map("destroy")}
,isc.A.destroy=function(){if(this.valuesManager)this.valuesManager.removeMember(this);this.$10k(this.items);this.Super("destroy",arguments)}
,isc.A.setHandleDisabled=function(_1){if(this.isDrawn()){if(this.redrawOnDisable)this.markForRedraw("setDisabled");this.disableKeyboardEvents(_1,null,true)}
var _2=this.getItems();for(var i=0;i<_2.length;i++){_2[i].updateDisabled()}}
,isc.A.disableKeyboardEvents=function(_1,_2,_3){this.Super("disableKeyboardEvents",arguments);if(!_3){if(_1){this.$551=this.getTabIndex();this.$vk(-1)}else{this.$vk(this.$551)}}}
,isc.A.applyFieldDefaults=function(_1){if(_1==null)return;for(var i=0;i<_1.length;i++){var _3=_1[i];if(_3==null)return}}
,isc.A.getEditorType=function(_1){return this.getClass().getEditorType(_1,this)}
,isc.A.setItems=function(_1,_2){if(_1!=null){for(var i=0;i<_1.length;i++){var _4=false;if(_1[i]==null){this.logWarn("Encountered empty entry in items array - removing this entry.")
_4=true}
if(isc.isA.Canvas(_1[i])){this.logWarn("Encountered a Canvas instance:"+_1[i]+" in the items "+"array - the DynamicForm items array should contain only FormItem "+"definitions. Removing this entry.");_4=true}
if(_4){_1.removeAt(i);i-=1}}}
_1=this.bindToDataSource(_1);if(!_1)_1=[];else if(_1==this.items)_1=_1.duplicate();if(this.items!=null&&this.items.length>0&&!_2)this.removeItems(this.items);this.$10l(_1,null,true,_2)}
,isc.A.setFields=function(_1){this.setItems(_1)}
,isc.A.getFields=function(){return this.items}
,isc.A.getItems=function(){return this.items}
,isc.A.visibleAtPoint=function(_1,_2,_3,_4){if(this.invokeSuper(isc.DynamicForm,"visibleAtPoint",_1,_2,_3,_4))
return true;var _5=this.items||[],_6={},_7=_5.indexOf(this.getFocusItem());for(var i=-1;i<_5.length;i++){var _9=i;if(i==-1){_9=_7}else if(_9==_7)continue;if(_9==-1)continue;var _10=_5[_9],_11=_10.containerWidget;if(_11==this||!_10.isDrawn()||!_10.isVisible())continue;var _12=_11.getID();if(_6[_12]==null){_6[_12]=_11.visibleAtPoint(_1,_2,_3,_4)}
if(!_6[_12])continue;var _13=_10.getPageLeft(),_14=_10.getPageTop();if(_13<=_1&&(_13+_10.getVisibleWidth())>=_1&&_14<=_2&&(_14+_10.getVisibleHeight())>=_2){return true}}
return false}
,isc.A.addItems=function(_1,_2){if(!isc.isAn.Array(_1))_1=[_1];if(this.dataSource){var _3=isc.DS.get(this.dataSource);for(var i=0;i<_1.length;i++){_1[i]=this.combineFieldData(_1[i]);var _5=_1[i].name;if(_5&&this.getItem(_5)){this.removeItem(_5)}}}
this.addFieldValidators(_1);if(_2==null||_2>this.items.length)_2=this.items.length;this.$10l(_1,_2)}
,isc.A.$10l=function(_1,_2,_3,_4){var _5=this.isDrawn(),_6=_5?this.getTabIndexSpan():null;this.applyFieldDefaults(_1);var _7=[];var _8=false,_9=false,_10=(this.sectionVisibilityMode==this.$9j);for(var _11=0;_11<_1.length;_11++){var _12=_1[_11];if(!_12){_1.removeItem(_11);_11--;continue}
if(_12.editorProperties)_12=isc.addProperties({},_12,_12.editorProperties);var _13=this.getEditorType(_12);_1[_11]=_12=this.createItem(_12,_13);if(_13==this.$9i)_8=true;if(isc.FileItem&&isc.isA.FileItem(_12)&&_9){this.logWarn("Attempting to creating a form with multiple FileItems. This is "+"not currently supported - only the first file type field value will "+"be committed on submission of this form.")}
if(isc.isA.SectionItem(_12)){_7.add(_12);if(_12.sectionExpanded&&_10)
this.$6l=_12}}
if(_3)this.items=_1
else this.items.addListAt(_1,_2);if(!_4)this.setItemValues(this.getValues(),false,true,_1);if(_8)this.encoding=isc.DynamicForm.MULTIPART_ENCODING;for(var i=0;i<_7.length;i++){var _15=_7[i],_16=_15.sectionExpanded;if(_16&&(!_10||(this.$6l==_15))){_15.expandSection()}else{_15.collapseSection()}}
this.$10m=true;var _17=this.getTabIndex();if(_5&&_17!=-1){this.$10o();var _18=this.getTabIndexSpan();if(_18>_6){var _19=this.$vy();if(_19){var _20=_19.getTabIndex();if(_20<(_17+_18)){_19.$v3((_17+_18)-_20)}}}}
this.markForRedraw("Form items added")}
,isc.A.copyKnownProperties=function(_1,_2,_3){var _4;for(var i=0;i<_3.length;i++){var _6=_3[i],_7=_2[_6];if(_7!==_4){_1[_6]=_7;delete _2[_6]}}}
,isc.A.createItem=function(_1,_2){if(_1.form!=null&&!(_1.form==this.getID()||_1.form!=this)){this.logWarn("Unsupported 'form' property ["+_1.form+"] set on item:"+_1+".  Ignoring.")}
if(_1.destroyed&&isc.isA.FormItem(_1)){this.logWarn("destroyed FormItem passed to setItems()/addItem(): FormItems cannot be "+"re-used with different DynamicForms")}
var _3=isc.FormItemFactory.getItemClassName(_1,_2,this),_4=isc.FormItemFactory.getItemClass(_3);if(!_4){this.logWarn("Problem initializing item: "+isc.Log.echo(_1)+" - derived FormItem class is: "+_3+".  Please make sure the relevant module is loaded");return}
var _5=_1;_1=_4.createRaw();_1.form=_1.containerWidget=_1.eventParent=this;if(isc.Browser.isIE&&this.canAlterItems){this.copyKnownProperties(_1,_5,this.$9k)}
if(this.autoChildItems){if(_1.ID==null)_1.ID=null;this.$d3(_4.Class,_1,_5)}else{_1.completeCreation(_5)}
_1.form=this;if(_1.destroyed)_1.destroyed=false;if(_1.shouldSaveValue&&(_1[this.fieldIdProperty]==null||isc.isAn.emptyString(_1[this.fieldIdProperty]))&&(_1.dataPath==null||isc.isAn.emptyString(_1.dataPath)))
{this.logWarn(_1.getClass()+" form item defined with no '"+this.fieldIdProperty+"' property - Value will not be saved."+" To explicitly exclude a form item from the set of values to "+"be saved, set 'shouldSaveValue' to false for this item.")
_1.shouldSaveValue=false}
return _1}
,isc.A.removeItems=function(_1){if(_1==null)return;if(!isc.isAn.Array(_1))_1=[_1];if(_1==this.items)_1=this.items.duplicate();_1=this.map("getItem",_1);this.items.removeList(_1);for(var i=0;i<_1.length;i++){var _3=_1[i];if(_3==null)continue;if(_3.items!=null){_1.addList(_3.items,i+1)}
if(this.$10n==_3)delete this.$10n;if(!this.items.contains(_3)&&isc.isA.FormItem(_3))_3.destroy()}
this.$10m=true;this.markForRedraw("Form items removed")}
,isc.A.addField=function(_1,_2){this.addItems(_1,_2)}
,isc.A.removeField=function(_1){this.removeItems(_1)}
,isc.A.addItem=function(_1,_2){this.addItems(_1,_2)}
,isc.A.removeItem=function(_1){this.removeItems(_1)}
,isc.A.addFields=function(_1,_2){return this.addItems(_1,_2)}
,isc.A.removeFields=function(_1){return this.removeItems(_1)}
,isc.A.$kk=function(_1,_2,_3,_4){if(this.canFocus==true)return true;var _5=this.getItems();for(var i=0;i<_5.length;i++){if(_5[i].$kk())return true}
return this.invokeSuper(isc.DynamicForm,"$kk",_1,_2,_3,_4)}
,isc.A.$10o=function(){var _1=this.items;if(!_1||_1.length==0)return;var _2=[],_3={};for(var i=0;i<_1.length;i++){var _5=_1[i],_6=_5.tabIndex;if(_6!=null&&_6!=-1){if(_2[_6]!=null&&!_3[_6]){this.logWarn("More than one item in this form have an explicitly specified tabIndex of '"+_6+"'. Tab order cannot be guaranteed within this form.");_3[_6]=true}
_2[_6]=_5}}
var _7=1;for(var i=0;i<_1.length;i++){var _5=_1[i];if(!isc.isA.FormItem(_5)){if(this.logIsDebugEnabled())
this.logDebug("$10o() fired before all form items have been initialized"+this.getStackTrace());continue}
if(!_5.$kk()||_5.tabIndex!=null||_5.globalTabIndex!=null){continue}
_7+=1;while(_2[_7]!=null){_7+=1}
_5.$10p=_7;if(isc.isA.CanvasItem(_5)){var _8=_5.canvas;if(_8&&_8.getTabIndexSpan){_7+=_8.getTabIndexSpan()}}}}
,isc.A.$v2=function(){return}
,isc.A.getTabIndexSpan=function(){var _1=this.items;var _2=1;if(!_1){return _2}
for(var i=0;i<_1.length;i++){var _4=_1[i];if(!isc.isA.FormItem(_4)){return _1.length}
if(!_4.$kk()||_4.globalTabIndex!=null){continue}
var _5=_4.tabIndex||_4.$10p;if(_5==null){this.$10o();_5=_4.$10p}
if(isc.isA.CanvasItem(_4)){var _6=_4.canvas,_7=0;if(_6&&_6.getTabIndexSpan)_7=_6.getTabIndexSpan();if(_7>1){_5+=_7-1}}
if(_5!=null&&_5>_2)_2=_5}
return _2}
,isc.A.$vk=function(){this.Super("$vk",arguments);if(this.isDrawn()&&this.isVisible()&&this.items){for(var i=0;i<this.items.length;i++){this.items[i].updateTabIndex()}}}
,isc.A.moved=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"moved",_1,_2,_3,_4);this.itemsMoved()}
,isc.A.parentMoved=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"parentMoved",_1,_2,_3,_4);this.itemsMoved()}
,isc.A.zIndexChanged=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"zIndexChanged",_1,_2,_3,_4);this.itemsZIndexChanged()}
,isc.A.parentZIndexChanged=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"parentZIndexChanged",_1,_2,_3,_4);this.itemsZIndexChanged()}
,isc.A.itemsMoved=function(){var _1=this.getItems();if(!_1)return;for(var i=0;i<_1.length;i++){if(_1[i].isVisible)_1[i].moved()}}
,isc.A.itemsVisibilityChanged=function(){var _1=this.getItems();if(!_1)return;for(var i=0;i<_1.length;i++){_1[i].visibilityChanged()}}
,isc.A.itemsZIndexChanged=function(){var _1=this.getItems();if(!_1)return;for(var i=0;i<_1.length;i++){_1[i].zIndexChanged()}}
,isc.A.scrollTo=function(_1,_2){var _3=this.getScrollLeft(),_4=this.getScrollTop();this.Super("scrollTo",arguments);if(_3!=this.getScrollLeft()||_4!=this.getScrollTop())this.itemsMoved()}
,isc.A.$1f=function(){if(this.canAnimateClip!=null)return this.canAnimateClip;return true}
,isc.A.setTitleOrientation=function(_1){this.titleOrientation=_1;this.$10m=true;this.markForRedraw()}
,isc.A.setNumCols=function(_1){this.numCols=_1;this.$10m=true;this.markForRedraw()}
,isc.A.setAutoComplete=function(_1){this.autoComplete=_1;for(var i=0;i<this.items.length;i++){this.items[i].$10q()}}
,isc.A.setValues=function(_1,_2){if(isc.isAn.Array(_1)){var _3=isc.isA.Object(_1[0]);this.logWarn("values specified as an array."+(_3?" Treating the first item in the array as intended values.":" Ignoring specified values (resetting to defaults)."));if(_3)_1=_1[0];else _1=null}
if(_1==null)_1={};this.$10r(_1);var _4=this.items;for(var i=0;i<_4.length;i++){if(_4[i].shouldSaveValue&&this.$425(_4[i])){_4[i].$426()}}
this.setItemValues(_1,null,_2);this.rememberValues();this.markForRedraw("setValues")}
,isc.A.$425=function(_1){if(!_1||!_1.displayField)return false;if(_1.optionDataSource!=null)return false;if(_1.getValueFieldName()!=_1.getFieldName())return false;return true}
,isc.A.setData=function(_1){this.setValues(_1)}
,isc.A.setDataSource=function(_1,_2){this.Super("setDataSource",arguments);this.clearErrors()}
,isc.A.rememberValues=function(){var _1=this.getValues(),_2=this.$10s={};this.$10t=[];var _3="__ref";for(var _4 in _1){if(isc.isA.Function(_1[_4]))continue;if(_4==_3)continue;if(isc.isA.Date(_1[_4])){_2[_4]=new Date();_2[_4].setTime(_1[_4].getTime())}else{_2[_4]=_1[_4]}
var _5=this.getItem(_4);if(_5&&_5.isSetToDefaultValue())this.$10t.add(_4)}
return _2}
,isc.A.resetValues=function(){this.clearErrors();var _1={};for(var _2 in this.$10s){if(this.$10t.contains(_2))continue;if(isc.isA.Date(_1[_2])&&isc.isA.Date(this.$10s[_2]))
_1[_2].setTime(this.$10s[_2].getTime());else
_1[_2]=this.$10s[_2]}
this.setValues(_1)}
,isc.A.clearValues=function(){this.setValues();var _1=this.getItems();for(var i=0;i<_1.length;i++){if(_1[i].shouldSaveValue==false)_1[i].setValue(null)}
this.clearErrors();this.rememberValues();this.markForRedraw("clearValues")}
,isc.A.valuesHaveChanged=function(){var _1=this.getValues(),_2=this.$10s,_3=false;if(!isc.isAn.Object(_2))_2={};for(var _4 in _1){if(isc.isA.Function(_1[_4]))continue;var _5=this.getItem(_4);if(_5!=null){_3=!_5.compareValues(_1[_4],_2[_4])}else{_3=!isc.DynamicForm.compareValues(_1[_4],_2[_4])}
if(_3)return true}
return _3}
,isc.A.getValues=function(){this.updateFocusItemValue();return this.values}
,isc.A.updateFocusItemValue=function(){var _1=this.getFocusItem();if(!this.$10u&&_1!=null&&_1.$10v()){_1.updateValue()}}
,isc.A.getData=function(){return this.getValues()}
,isc.A.getValuesAsCriteria=function(_1,_2){var _3=this.$657();var _4=this.getFields().duplicate();for(var i=0;i<_4.length;i++){if(!_4[i].shouldSaveValue){_4[i]=null}else if(_4[i].operator){_2=true}}
_4.removeEmpty();var _6=this.dataSource;if(!_6){if(this.grid&&this.grid.sourceWidget)_6=this.grid.sourceWidget.dataSource}
_6=isc.DataSource.getDataSource(_6);if(!_2||!_6){if(_1)return _3;return isc.DataSource.filterCriteriaForFormValues(_3)}
var _7={_constructor:"AdvancedCriteria",operator:this.operator}
var _8=[];for(i=0;i<_4.length;i++){var _9=_4[i];var _10=_4[i].criteriaField?_4[i].criteriaField:_4[i].name;if(_6.getField(_10)==null)continue
var _11=_9.getCriterion();if(_11)_8.add(_11)}
_7.criteria=_8;return _7}
,isc.A.$657=function(){var _1={};var _2=this.getFields();for(var i=0;i<_2.length;i++){if(!_2[i].shouldSaveValue)continue;var _4=_2[i].getCriteriaFieldName();if(_4)_1[_4]=_2[i].getCriteriaValue()}
return _1}
,isc.A.getFilterCriteria=function(){return this.getValuesAsCriteria()}
,isc.A.getValuesAsAdvancedCriteria=function(_1){return this.getValuesAsCriteria(_1,true)}
,isc.A.getItem=function(_1){if(isc.isA.FormItem(_1))return _1;var _2=isc.Class.getArrayItem(_1,this.items,this.fieldIdProperty);if(_2!=null)return _2;_2=isc.Class.getArrayItem(_1,this.items,"dataPath");if(_2!=null)return _2;if(isc.isA.Number(_1-1)){return this.items[parseInt(_1)]}
return null}
,isc.A.getField=function(_1){return this.getItem(_1)}
,isc.A.getSubItem=function(_1){return this.getItem(_1)}
,isc.A.getItemById=function(_1){var _2;if(isc.isA.String(_1)){_2=window[_1]}else _2=_1;if(isc.isA.FormItem(_2))return _2;return null}
,isc.A.getValue=function(_1){var _2=this.getItem(_1);if(_2)return _2.getValue();return this.$70p(_1)}
,isc.A.$70p=function(_1){return isc.DynamicForm.$70o(_1,this.values)}
,isc.A.setValue=function(_1,_2){var _3=this.getItem(_1);if(_3!=null)return _3.setValue(_2);else if(this.values!=null){this.$10w(_1,_2);return _2}}
,isc.A.clearValue=function(_1){var _2=this.getItem(_1);if(_2!=null)_2.clearValue();else if(this.values)isc.DynamicForm.$70m(_1,this.values)}
,isc.A.showItem=function(_1){var _2=this.getItem(_1);if(_2!=null)return _2.show()}
,isc.A.hideItem=function(_1){var _2=this.getItem(_1);if(_2!=null)return _2.hide()}
,isc.A.saveItemValue=function(_1,_2){if(_1.shouldSaveValue==false)return;var _3=_1.getDataPath()||_1.getFieldName();if(_3!=null){this.$10w(_3,_2)}else{return}
if(this.$425(_1)&&(_1.displayField!=_3)){var _4=_1.mapValueToDisplay(_2);this.setValue(_1.displayField,_4)}
_1.$10x()}
,isc.A.$10w=function(_1,_2){isc.DynamicForm.$70n(_1,_2,this.values);if(!this.selectionComponent&&this.valuesManager!=null){if(isc.isA.ValuesManager(this.valuesManager)&&this.valuesManager.members&&this.valuesManager.members.contains(this))
{this.valuesManager.$10y(_1,_2,this)}}}
,isc.A.clearItemValue=function(_1){var _2=_1.getDataPath()||_1.getFieldName();isc.DynamicForm.$70m(_2,this.values);if(!this.selectionComponent&&this.valuesManager){this.valuesManager.$10z(_2,this)}}
,isc.A.$10r=function(_1){this.values=_1;if(!this.selectionComponent&&this.valuesManager!=null){var _2=isc.getKeys(this.values);for(var i in _1){this.valuesManager.$10y(i,_1[i],this);_2.remove(i)}
for(var i=0;i<_2.length;i++){this.valuesManager.$10z(_2[i],this)}}}
,isc.A.getSavedItemValue=function(_1){if(_1.shouldSaveValue==false)return null;var _2=_1.getDataPath()||_1.getFieldName();return this.$70p(_2)}
,isc.A.resetValue=function(_1){var _2=this.getItem(_1);return(_2?_2.resetValue():null)}
,isc.A.getValueMap=function(_1){var _2=this.getItem(_1);return(_2?_2.getValueMap():null)}
,isc.A.setValueMap=function(_1,_2){var _3=this.getItem(_1);return(_3?_3.setValueMap(_2):null)}
,isc.A.getOptions=function(_1){return this.getValueMap(_1)}
,isc.A.setOptions=function(_1,_2){return this.setValueMap(_1,_2)}
,isc.A.getForm=function(_1){var _2=(_1==null?[this.getFormID()]:arguments);return this.Super("getForm",_2)}
,isc.A.getFormID=function(){return this.$qs(this.$9l)}
,isc.A.getSerializeableFields=function(_1,_2){_1.addList(["items"]);return this.Super("getSerializeableFields",arguments)}
,isc.A.expandSection=function(_1){var _2=this.getItem(_1);if(isc.isA.SectionItem(_2))_2.expandSection()}
,isc.A.collapseSection=function(_1){var _2=this.getItem(_1);if(isc.isA.SectionItem(_2))_2.collapseSection()}
,isc.A.$100=function(_1){if(this.sectionVisibilityMode=="mutex"&&this.$6l&&this.$6l!=_1)
{this.$6l.collapseSection()}
this.$6l=_1}
,isc.A.$101=function(_1){}
,isc.A.getErrors=function(){return this.errors}
,isc.A.getFieldErrors=function(_1){if(!this.errors)return null;if(isc.isA.FormItem(_1))_1=_1.getFieldName();return this.errors[_1]}
,isc.A.setErrors=function(_1,_2){this.errors=isc.DynamicForm.formatValidationErrors(_1);var _3=false,_4={};for(var _5 in this.errors){var _6=this.getItem(_5);if(!_6||!_6.visible){_4[_5]=this.errors[_5];_3=true}}
if(_2)this.showErrors(this.errors,_4)}
,isc.A.setError=function(_1,_2){var _3=this.errors[_1];if(!_3)this.errors[_1]=_2;else{if(isc.isA.String(_3))this.errors[_1]=[_3,_2];else this.errors[_1].add(_2)}}
,isc.A.addFieldErrors=function(_1,_2,_3){if(!this.errors)this.errors={};this.addValidationError(this.errors,_1,_2);if(_3)this.showFieldErrors(_1)}
,isc.A.setFieldErrors=function(_1,_2,_3){if(this.errors==null)this.errors={};this.errors[_1]=_2;if(_3)this.showFieldErrors(_1)}
,isc.A.clearFieldErrors=function(_1,_2){if(this.errors==null)return;if(!this.errors[_1])return;delete this.errors[_1];if(_2){this.showFieldErrors(_1)}}
,isc.A.clearErrors=function(_1){this.setErrors({},_1)}
,isc.A.hasErrors=function(){var _1=this.errors;if(!_1)return false;for(var _2 in _1){if(_1[_2]!=null)return true}
return false}
,isc.A.hasFieldErrors=function(_1){var _2=this.errors;return(_2&&_2[_1]!=null)}
,isc.A.draw=function(_1,_2,_3,_4){if(isc.$cv)arguments.$cw=this;if(!this.readyToDraw())return this;this.invokeSuper(isc.DynamicForm,this.$ny,_1,_2,_3,_4);this.$102();var _5=this.autoFocus,_6=(!_5?this.$9m:this.$9o);this.$10u=true;isc.Page.setEvent(isc.EH.IDLE,this,isc.Page.FIRE_ONCE,_6);if(this.position==isc.Canvas.RELATIVE){isc.Page.setEvent(isc.EH.LOAD,this,isc.Page.FIRE_ONCE,"$103")}
return this}
,isc.A.$us=function(){if(isc.Browser.isSafari){var _1=this.getItems();if(this.isDrawn()&&_1){for(var i=0;i<_1.length;i++){_1[i].$104();if(this.isDirty())break}}}
return this.Super("$us",arguments)}
,isc.A.$9n=function(){this.setItemValues(null,true);this.rememberValues();delete this.$10u}
,isc.A.$9p=function(){this.$9n();this.focus()}
,isc.A.redraw=function(){this.$107();this.$106();this.Super("redraw",arguments);this.$108();this.setItemValues(null,true);var _1,_2,_3;if(isc.Browser.isMoz){_3=this.getClipHandle();if(_3){_1=_3.scrollLeft;_2=_3.scrollTop}}
if(isc.Browser.isMoz){if(_1!=null&&_3.scrollLeft!=_1)
_3.scrollLeft=_1;if(_2!=null&&_3.scrollTop!=_2)
_3.scrollTop=_2}
this.itemsMoved()}
,isc.A.$102=function(){var _1=this.items;for(var i=0;i<_1.length;i++){if(_1[i]&&_1[i].visible)_1[i].drawn()}}
,isc.A.$108=function(){var _1=this.items;for(var i=0;i<_1.length;i++){var _3=_1[i];if(!_3)continue;if(_3.visible){_3.isDrawn()?_3.redrawn():_3.drawn()}else if(_3.isDrawn()){_3.cleared()}}}
,isc.A.$11a=function(){var _1=this.items;for(var i=0;i<_1.length;i++){if(_1[i].isDrawn())_1[i].cleared()}}
,isc.A.$107=function(){var _1=this.items;for(var i=0;i<_1.length;i++){var _3=_1[i];if(!_3)continue;if(_3.visible&&_3.isDrawn())_3.redrawing()}}
,isc.A.modifyContent=function(){this.$103()}
,isc.A.$103=function(){return this.$11c("placeCanvas")}
,isc.A.$11c=function(_1){if(!isc.CanvasItem)return;for(var i=0;i<this.items.length;i++){var _3=this.items[i];if(_3&&isc.isA.CanvasItem(_3))_3[_1]()}}
,isc.A.redrawFormItem=function(_1,_2){var _3=this.getItems();if(!_1||!_3.contains(_1))return;this.$10m=true;this.markForRedraw(_1.ID+": "+(_2?_2:"redrawFormItem"))}
,isc.A.getElementValues=function(){var _1={};for(var i=0;i<this.items.length;i++){var _3=this.items[i],_4=_3.getDataElement()?_3.getDataElement().value:"[no element]";_1[_3[this.fieldIdProperty]]=_4}
return _1}
,isc.A.setItemValues=function(_1,_2,_3,_4){var _5=(_1==null);if(_5)_1=this.getValues();if(_1==null)_1={};_4=_4||this.items;for(var _6=0;_6<_4.length;_6++){var _7=_4[_6],_8=_7.getFieldName(),_9=_7.getDataPath(),_10=_7.isSetToDefaultValue(),_11;if(_9){var _12=_9.split(isc.slash),_13=_1;for(var i=0;i<_12.length-1;i++){_13=_13[_12[i]];if(_13==null)break}
if(_13!=null)_11=_13[_12.last()]}else if(_8)_11=_1[_8];if(_2&&isc.CanvasItem&&isc.isA.CanvasItem(_7)&&!_7.$11d())
{continue}
var _15,_16=((!_8&&!_9)||_11===_15);if(_7.shouldSaveValue==false){if(!_16){this.logInfo("DynamicForm.setValues() passed a value for '"+_7[this.fieldIdProperty]+"'."+" The corresponding form item was declared with 'shouldSaveValue' set to "+" false to exclude its value from the form's values object."+" Setting 'shouldSaveValue' to true for this item."+"\n[To avoid seeing this message in the future, set 'shouldSaveValue'"+" to true for any form items whose values are to be managed via "+" form.setValues() / form.getValues().]")
_7.shouldSaveValue=true}else{var _17=(_10?null:_7._value);_7.setValue(_17,(_10?false:_2));continue}}
if(_16||(_5&&_10)){var _18;if(!_3)_7.clearValue();else if(_3&&_10&&_7._value!==_18){_7.saveValue(_7._value,true)}}else{_7.setValue(_11,true)}}}
,isc.A.$11e=function(){return this.itemLayout==this.$9q}
,isc.A.getInnerHTML=function(){var _1=isc.StringBuffer.create();if(this.writeFormTag&&!this.isPrinting)_1.append(this.getFormTagStartHTML());var _2=this.values,_3=this.items;var _4=false;for(var _5=0;_5<_3.length;_5++){var _6=_3[_5],_7=_6.visible;if(_6.showIf){isc.Func.replaceWithMethod(_6,this.$9r,this.$9s);var _8=_6.getValue();_7=(_6.showIf(_6,_8,this,_2)==true)}
if(_7&&this.isPrinting){if(_6.shouldPrint!=null){_7=_6.shouldPrint}else if(_7&&this.currentPrintProperties.omitControls){var _9=this.currentPrintProperties.omitControls;for(var i=0;i<_9.length;i++){var _11=_9[i];if(isc.isA[_11]&&isc.isA[_11](_6)){_7=false}}}}
if(_7!=_6.visible){_6.visible=_7;if(!_6.alwaysTakeSpace)_4=true}}
if(_4||this.$10m)isc.Canvas.invalidateTableResizePolicy(_3);this.$10m=false;this.setRequiredIf();if(this.flattenItems){var _12=null;for(var _5=0;_5<_3.length;_5++){var _6=_3[_5];if(_6.visible||_6.alwaysTakeSpace)_12++;if(_6.showTitle&&_6.titleOrientation!="top")
_12++;_6.$58l=_6.colSpan||null;_6.colSpan=null}
if(_12){this.numCols=_12;this.$10m=true;this.markForRedraw()}}
if(this.$11e()){_1.append(this.getAbsPosHTML());_1.append(this.$9t);return _1.release()}
_1.append(this.getTableStartHTML());if(this.titleWidth==this.$pa&&!this.colWidths){this.colWidths=[];for(var i=0;i<this.numCols;i++)this.colWidths[i]=this.$pa}
var _13;if(this.colWidths){_13=this.colWidths;if(_13.length>this.numCols){this.logWarn("colWidths Array longer than numCols, using only first "+this.numCols+" column widths");_13=_13.slice(0,this.numCols)}else if(_13.length<this.numCols){this.logWarn("colWidths Array shorter than numCols, remaining columns get '*' size");for(var i=_13.length;i<this.numCols;i++)_13[i]=isc.star}}else{_13=[];var _14=this.getInnerContentWidth();_14-=(this.cellBorder!=null?this.cellBorder:0);var _15=Math.floor(this.numCols/ 2),_16=_14-(_15*this.titleWidth),_17=Math.floor(_16/(this.numCols-_15));_17=Math.max(this.minColWidth,_17);for(var i=0;i<_15;i++){_13.add(this.titleWidth);_13.add(_17)}
if((this.numCols%2)!=0)_13.add(_17);if(this.logIsInfoEnabled(this.$9u)){this.logInfo("totalWidth: "+_14+", generated colWidths: "+_13,this.$9u)}}
var _18=this.getInnerContentWidth(),_19=this.getInnerContentHeight();if(this.cellSpacing!=0){if(isc.Browser.isMoz)_19-=2*this.cellSpacing;else if(isc.Browser.isSafari)_19-=this.cellSpacing}
_3.$8q=this.defaultRowHeight;isc.Canvas.applyTableResizePolicy(_3,_18,_19,this.numCols,_13);var _20=false;if(isc.CanvasItem){for(var i=0;i<_3.length;i++){var _6=_3[i];if(isc.isA.CanvasItem(_6)&&_6.checkCanvasOverflow()){if(!_20&&this.logIsInfoEnabled(this.$9u)){this.logInfo("CanvasItem: "+_6+" overflowed, rerunning policy",this.$9u)}
_20=true}}}
if(_20){isc.Canvas.applyTableResizePolicy(_3,_18,_19,this.numCols,_13,null,true)}
_13=_3.$8r;for(var _21=0;_21<_13.length;_21++){var _22=_13[_21];_1.append(this.$9v,_22,this.$oa)}
_1.append(this.$9w);for(var _21=0;_21<_13.length;_21++){var _18=_13[_21];_18-=(this.cellSpacing!=null?(2*this.cellSpacing):0);this.$9x[3]=(isc.FormItem?isc.FormItem.getPrototype().baseStyle:null);var _23=isc.Browser.isIE?1:0,_24=this.$9x.join(isc.emptyString);_1.append(_24,this.fixedColWidths?isc.Canvas.spacerHTML(_18,_23):null,this.$9y)}
_1.append(this.$5t);if(this.autoSendTarget&&this.target)_1.append(this.$11f());var _25=[];for(var _5=0,_26=_3.length;_5<_26;_5++){var _6=_3[_5];if(!_6)continue;var _7=_6.visible;_6.$10x();if(this.logIsDebugEnabled())this.logDebug("Drawing FormItem: "+_6);if(!_6.alwaysTakeSpace&&!_7)continue;if((_6.rowSpan==0||_6.colSpan==0)&&_5<_26-1){_25.add(_6);continue}
var _27=_6.getFieldName(),_28=_6.getErrors(),_8=_6.getValue(),_29=this.getTitleOrientation(_6);if(isc.is.emptyString(_28))_28=null;if(_6.$8l||_5==0){if(_5!=0)_1.append(this.$5t);if(_6.$8n>0){for(var i=0;i<_6.$8n;i++)_1.append(this.$5s,this.$5t)}
_1.append(this.$5s);if(_6.$8o>0){for(var i=0;i<_6.$8o;i++)_1.append(this.$5u,this.$5v)}}
if(_29==isc.Canvas.LEFT){_1.append(this.getTitleCellHTML(_6,_28))}
_1.append(this.getCellStartHTML(_6,_28));if(_7&&_29==isc.Canvas.TOP){_1.append(this.getTitleSpanHTML(_6,_28),this.$9z)}
var _30=(_7&&_28&&this.showInlineErrors);if(_30&&_6.getErrorOrientation()==isc.Canvas.TOP){_1.append(this.getItemErrorHTML(_6,_28))}
if(_25.length>0){for(var m=0;m<_25.length;m++){if(!_25[m].visible)continue;_1.append(_25[m].getInnerHTML(_25[m].getValue()))}
_25.length=0}
if(_7){_1.append(_6.getInnerHTML(_8,true,this.showInlineErrors))}else _1.append(isc.Canvas.spacerHTML(_6.width,_6.height));if(_30&&_6.getErrorOrientation()==isc.Canvas.BOTTOM){_1.append(this.getItemErrorHTML(_6,_28))}
_1.append(this.getCellEndHTML(_6,_28));if(_29==isc.Canvas.RIGHT){_1.append(this.getTitleCellHTML(_6,_28))}}
if(_3.length>0)_1.append(this.$5t);if(this.writeFormTag&&!this.isPrinting)_1.append(this.$90);else _1.append("</TABLE>");return _1.release()}
,isc.A.getPrintChildren=function(){return null}
,isc.A.createErrorItem=function(){var _1=isc.addProperties({cellStyle:this.errorItemCellStyle},this.errorItemDefaults,this.errorItemProperties);this.addItems([_1],0);this.$52o=this.getItem(0)}
,isc.A.getErrorsHTML=function(_1){if(!_1||isc.isAn.emptyObject(_1))return isc.emptyString;var _2=this.errorsPreamble;_2+="<UL>";for(var _3 in _1){var _4=this.getItem(_3),_5;if(_4)_5=_4.getErrorMessage(_1[_3])
else{_5=_1[_3];if(isc.isAn.Array(_5))
_5="<ul><li>"+_5.join("</li><li>")+"</li></ul>"}
_2+="<LI>"+(_4?_4.getTitle():_3)+" : "+_5+"</LI>"}
_2+="</UL>";return _2}
,isc.A.getItemErrorHTML=function(_1,_2){return _1.getErrorHTML(_2)}
,isc.A.$11f=function(){this.$91[1]=this.autoSendTargetFieldName;this.$91[3]=this.target;return this.$91.join(isc.emptyString)}
,isc.A.getCellStartHTML=function(_1,_2){var _3=_1.getColSpan(),_4=_1.getRowSpan();if(_3==0)_3=1;if(_4==0)_4=1;if(_3=="*"){var _5=(_1.$8m?_1.$8m[0]:0);_3=(this.numCols-_5)}
var _6=_1.getCellStyle();var _7=_1.$8t?_1.$8t[1]:null;if(isc.isA.Number(_7)&&this.cellSpacing!=0)_7-=2*this.cellSpacing;return this.$11g((_1.align?_1.align:((this.form?this.form.isRTL():this.isRTL())?isc.Canvas.RIGHT:isc.Canvas.LEFT)),_6,_4,_3,null,(this.fixedRowHeights?_7:null),null,_1.cssText,(this.form?this.form.getID():this.getID()),_1.getItemID(),_1.getFormCellID())}
,isc.A.$11g=function(_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12){var _13=isc.StringBuffer.create(),_14=isc.$ad;_13.append("<TD ALIGN=",_1,(_2!=null?" CLASS='"+_2+"'":_14)," STYLE='",(_8!=null?_8:_14),"'",(_3>1?" ROWSPAN="+_3:_14),(_4>1?" COLSPAN="+_4:_14),(_5!=null?" WIDTH="+_5:_14),(_6!=null?" HEIGHT="+_6:_14),(_7!=null?_7:_14));if(_11){_13.append(" ID=",_11," ")}
if(_10&&_9){_13.append(isc.DynamicForm.$89,"='",_10,"'")}
_13.append(_12?"><NOBR>":">");return _13.release()}
,isc.A.getCellEndHTML=function(_1,_2){return this.$11h()}
,isc.A.$11h=function(_1){return _1?"</NOBR></TD>":"</TD>"}
,isc.A.getTitleOrientation=function(_1){if(_1&&!_1.shouldShowTitle())return null;return(_1?_1.titleOrientation:null)||this.titleOrientation||isc.Canvas.LEFT}
,isc.A.getTitleAlign=function(_1){var _2=this.form||this;return(_1.titleAlign?_1.titleAlign:this.titleAlign?this.titleAlign:this.isRTL()?isc.Canvas.LEFT:isc.Canvas.RIGHT)}
,isc.A.getTitleVAlign=function(_1){var _2=(_1.titleVAlign?_1.titleVAlign:this.titleVAlign?this.titleVAlign:isc.Canvas.CENTER);return(_2==isc.Canvas.CENTER?isc.Canvas.MIDDLE:_2)}
,isc.A.getTitleHeight=function(_1){var _2=this.form||this;return(_1.titleHeight!=null?_1.titleHeight:this.titleHeight)}
,isc.A.getTitleSpanHTML=function(_1,_2){var _3=isc.StringBuffer.create();_3.append("<SPAN ",this.$11i(_1)," CLASS='",_1.getTitleStyle(),"' ALIGN=",this.getTitleAlign(_1),">");_3.append(this.getTitleHTML(_1,_2));_3.append("</SPAN>");return _3.release()}
,isc.A.shouldClipTitle=function(_1){if(!_1||!_1.form==this)return false;return(_1.clipTitle!=null?_1.clipTitle:this.clipItemTitles)}
,isc.A.getTitleCellHTML=function(_1,_2){var _3=isc.StringBuffer.create(),_4=_1.getTitleStyle(),_5=this.getTitleAlign(_1),_6=this.getTitleVAlign(_1);var _7=this.$99;_7[1]=this.$11i(_1);_7[3]=_4;_7[5]=_5;_7[7]=_6;if(_1.getRowSpan()>1)_7[9]=" ROWSPAN="+_1.getRowSpan();else _7[9]=null;_3.append(_7.join(isc.emptyString));_3.append(this.getTitleCellInnerHTML(_1,_2));_3.append(this.$98);return _3.release()}
,isc.A.getTitleCellInnerHTML=function(_1,_2){var _3=isc.StringBuffer.create(),_4=_1.getTitleStyle(),_5=this.getTitleAlign(_1),_6=_1.$8u||null,_7=_1.$8t?_1.$8t[1]:null,_8=this.shouldClipTitle(_1),_9=(_1.wrapTitle!=null?_1.wrapTitle:(this.wrapItemTitles!=null?this.wrapItemTitles:!_8));if(_7){if(this.cellSpacing)_7-=2*this.cellSpacing;var _10,_11;if(_4){_10=isc.Element.$tt(_4,true);_11=isc.Element.$tu(_4,true)}
if(_10==null)_10=this.cellPadding||0;if(_11==null)_11=this.cellPadding||0;_7-=(_10+_11)
if(_4)_7-=isc.Element.$ym(_4)}
if(_6){if(this.cellSpacing)_6-=2*this.cellSpacing;var _12,_13;if(_4){_12=isc.Element.$tr(_4,true);_13=isc.Element.$ts(_4,true)}
if(_12==null)_12=this.cellPadding||0;if(_13==null)_13=this.cellPadding||0;_6-=(_12+_13)
_6-=isc.Element.$yn(_4)}
var _14=isc.Browser.isMoz?this.$94:this.$92,_15=isc.Browser.isMoz?this.$11j:this.$93;if(_8){var _16=this.$10a;if(_9||!isc.Browser.isMoz)_16[1]=null;else _16[1]="white-space:nowrap;";if(_6!=null)_16[2]=_15+_6+"px;"
else _16[2]=null;if(_7!=null)_16[3]=_14+_7+"px;"
else _16[3]=null;_3.append(_16.join(isc.emptyString));if(!isc.Browser.isMoz){var _17=this.$10b;_17[1]=_7;_17[3]=_4;_17[5]=_5;if(!_9)_17[7]=this.$95
else _17[7]=null;_3.append(_17.join(isc.emptyString))}}else if(!_9){_3.append(this.$95)}
_3.append(this.getTitleHTML(_1,_2));if(_8){if(!isc.Browser.isMoz)_3.append(this.$96);_3.append(this.$97)}
return _3.release()}
,isc.A.$11i=function(_1){if(!isc.DynamicForm.$11k){isc.DynamicForm.$11l=[" ",isc.DynamicForm.$89,"='",null,"' ",isc.DynamicForm.$9a,"='",isc.DynamicForm.$9g,"' ","ID="]}
isc.DynamicForm.$11l[3]=_1.getItemID();isc.DynamicForm.$11l[10]=this.$427(_1);return isc.DynamicForm.$11l.join(isc.emptyString)}
,isc.A.$427=function(_1){return this.$qs(_1.getID()+this.$63z)}
,isc.A.getTitleCell=function(_1){if(!this.isDrawn())return null;_1=this.getItem(_1);if(!_1)return null;return isc.Element.get(this.$427(_1))}
,isc.A.updateTitleCellState=function(_1){var _2=this.getTitleCell(_1);if(_2==null)return;_1=this.getItem(_1);_2.className=_1.getTitleStyle();_2.innerHTML=this.getTitleCellInnerHTML(_1,_1.getErrors())}
,isc.A.getTitleHTML=function(_1,_2){var _3=isc.StringBuffer.create();var _4=_1.visible?_1.getTitleHTML():null;if(_4){var _5=this.isRequired(_1),_6=this.getTitleOrientation(_1),_7=(_6==isc.Canvas.LEFT||_6==isc.Canvas.TOP);_3.append((_5&&this.hiliteRequiredFields?(_7?this.requiredTitlePrefix:this.requiredRightTitlePrefix):(_7?this.titlePrefix:this.rightTitlePrefix)),_4,(_5&&this.hiliteRequiredFields?(_7?this.requiredTitleSuffix:this.requiredRightTitleSuffix):(_7?this.titleSuffix:this.rightTitleSuffix)))}else{_3.append("&nbsp;")}
return _3.release()}
);isc.evalBoundary;isc.B.push(isc.A.getFormTagStartHTML=function(){var _1=this.$10c,_2=this.getFormID(),_3=this.getID();_1[3]=_2;if(this.$11e())_1[4]=this.$10e;else _1[4]=null;_1[6]=this.method;_1[8]=this.action;if(this.isMultipart())_1[10]=isc.DynamicForm.MULTIPART_ENCODING;else _1[10]=isc.DynamicForm.NORMAL_ENCODING;if(this.target!=null){_1[11]=this.$10f;_1[12]=this.target;_1[13]=this.$ob}else{_1[11]=null;_1[12]=null;_1[13]=null}
_1[15]=_3;_1[17]=_3;return _1.join(isc.emptyString)}
,isc.A.getTableStartHTML=function(){var _1=isc.isA.DynamicForm(this)?this.$10g:isc.DynamicForm.getPrototype().$10g;_1[1]=this.$11n();_1[4]=(this.getInnerContentWidth!=null?this.getInnerContentWidth():this.getInnerWidth());_1[6]=this.cellSpacing;_1[8]=this.cellPadding;_1[10]=this.cellBorder;return _1.join(isc.emptyString)}
,isc.A.$11n=function(){return this.$qs(this.$10h)}
,isc.A.$11o=function(){return isc.Element.get(this.$11n())}
,isc.A.layoutChildren=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"layoutChildren",_1,_2,_3,_4);var _5=this.getItems();if(!_5)return;for(var i=0;i<_5.length;i++){var _7=_5[i].width,_8=_5[i].height;if((isc.isA.String(_7)&&(_7.contains("%")||_7.contains("*")))||(isc.isA.String(_8)&&(_8.contains("%")||_8.contains("*"))))
{this.markForRedraw("size change with dynamic size children");break}}}
,isc.A.getAbsPosHTML=function(){var _1=isc.SB.create();for(var _2=0,_3=this.items.length;_2<_3;_2++){var _4=this.items[_2];if(!_4)continue;_4.$10x();if(!_4.visible)continue;_1.append(_4.getStandaloneItemHTML(_4.getValue()))}
return _1.release()}
,isc.A.getScrollWidth=function(_1){if(this.$qz){this.$qz=null;this.adjustOverflow("widthCheckWhileDeferred")}
if(!_1&&this.$su!=null)return this.$su;var _2;if(!isc.Browser.isIE||!this.$11e()||!(this.isDrawn()||this.handleDrawn())||this.items==null)
{_2=isc.Canvas.$b4.getScrollWidth.call(this,_1)}else{_2=0;for(var i=0;i<this.items.length;i++){var _4=this.items[i];if(_4.visible==false||!_4.isDrawn())continue;var _5=_4.getAbsDiv();if(_5){var _6=_5.scrollWidth+_4.$11p(_4.left);if(_6>_2)_2=_6}}}
this.$su=_2;return _2}
,isc.A.getScrollHeight=function(_1){if(this.$qz){this.$qz=null;this.adjustOverflow("heightCheckWhileDeferred")}
if(!_1&&this.$sz!=null)return this.$sz;var _2;if(!isc.Browser.isIE||!this.$11e()||!(this.isDrawn()||this.handleDrawn())||this.items==null)
{_2=isc.Canvas.$b4.getScrollHeight.call(this,_1)}else{_2=0;for(var i=0;i<this.items.length;i++){var _4=this.items[i];if(_4.visible==false||!_4.isDrawn())continue;var _5=_4.getAbsDiv();if(_5){var _6=_5.scrollHeight+_4.$11p(_4.top,true);if(_6>_2)_2=_6}}}
this.$sz=_2;return _2}
,isc.A.$11q=function(){return this.canSubmit||this.isMultipart()||(this.action!=isc.DynamicForm.getPrototype().action)}
,isc.A.submitForm=function(){if(!this.$11q()){this.logWarn("Attempt to perform direct submission on DynamicForm where this.canSubmit "+"is false. Please set this property to true, or use the standard databinding "+"interfaces to send data to the server.")}
if(this.getFileItemForm()!=null){this.logWarn("Performing a direct submission on a DynamicForm containing a FileItem. "+"Note: This item's value will not be submitted to the server.  FileItems "+"are intended for use with databound forms backed by the SmartClient server "+"only.  If you are not using the SmartClient Databinding subsystem, "+"use an UploadItem rather than a FileItem to submit a file as part of a raw "+"HTTP request. Otherwise use saveData() rather than a direct call to "+"submitForm() to save the full set of values for the form.")}
var _1=this.getForm();if(!_1)return;if(_1.action!=this.action)_1.action=this.action;try{return _1.submit()}catch(e){this.logWarn("Form submission was unsuccessful. In some browsers this can occur when "+"an upload item is present and has an invalid value.");this.formSubmitFailed()}}
,isc.A.formSubmitFailed=function(){isc.warn(this.formSubmitFailedWarning);var _1=this.getValues()._transaction;if(_1!=null&&isc.RPCManager&&isc.XMLTools){var _2=isc.XMLTools.parseXML(this.getValues()._transaction),_3;if(_2)_3=isc.XMLTools.selectNumber(_2,"//transactionNum");if(_3!=null){isc.RPCManager.doClearPrompt(_3);isc.RPCManager.clearTransaction(_3)}}}
,isc.A.setAction=function(_1,_2){this.action=_1;var _3=this.getForm();if(_3)_3.action=_1;this.$66g=!_2}
,isc.A.setTarget=function(_1){this.target=_1;var _2=this.getForm();if(_2)_2.target=_1}
,isc.A.setMethod=function(_1){this.method=_1;var _2=this.getForm();if(_2)_2.method=_1}
,isc.A.getFileItemForm=function(){if(!isc.FileItem)return null;var _1=this.getItems()||[];for(var i=0;i<_1.length;i++){if(isc.isA.FileItem(_1[i]))return _1[i].canvas}
return null}
,isc.A.$10d=function(){return false}
,isc.A.validate=function(_1,_2,_3,_4){if(this.disableValidation)return true;if(this.dataSource&&this.dataSource.useLocalValidators!=null&&this.useLocalValidators==false)return true;var _5=this.hasErrors(),_6=false,_7=null,_8=this.getForm(),_9=false;var _10={},_11={},_12=this.getValues(),_13=(_1&&!_2&&this.dataSource)?isc.addProperties({},this.getDataSource().getFields()):null;var _14={unknownErrorMessage:this.unknownErrorMessage,serverValidationMode:"full"};if(_3)
_14.typeValidationsOnly=_3;for(var _15=0;_15<this.items.length;_15++){var _16=false,_17=this.items[_15],_18=_17.getFieldName(),_19=_17.getValue(),_20=!_17.visible||isc.isA.HiddenItem(_17);if(_20&&!_1)continue;if(_17.validators!=null){if(!isc.isAn.Array(_17.validators)){_17.validators=[_17.validators]}
var _21=this.validateField(_17,_17.validators,_19,_12,_14);if(_21!=null){if(_21.errors!=null){_16=this.addValidationError(_10,_18,_21.errors);if(_16)_6=true}
if(_21.resultingValue!=null){_19=_12[_18]=_21.resultingValue;_9=true}}}
if(_20){if(_16)_11[_18]=_10[_18]}else if(_7==null&&_6)_7=_17;if(_13)delete _13[_18]}
if(_13){_14={dontValidateNullValues:true};for(var i in _13){var _23=_13[i],_24=i,_25=_23.validators,_19=_12[_24];if(_25!=null){var _19=_12[_24];var _21=this.validateField(_23,_25,_19,_12,_14);if(_21!=null&&_21.errors!=null){this.addValidationError(_10,_24,_21.errors)}}
if(_10[_24]!=null)_11[_24]=_10[_24]}}
if(_6)this.logInfo("Validation errors: "+isc.Log.echoAll(_10));if(_4)return(_6?_10:true);this.setErrors(_10);if(_9){this.setItemValues(_12);for(var _26 in _12){if(this.getItem(_26)==null)this.$10w(_26,_12[_26])}}
if(_6||_5)this.showErrors(_10,_11);return!_6}
,isc.A.valuesAreValid=function(_1,_2){var _3=this.validate(_1,null,null,true);if(_3===true){return(_2?null:true)}else{return(_2?_3:false)}}
,isc.A.getValidatedValues=function(){if(!this.validate())return null;return this.getValues()}
,isc.A.showErrors=function(_1,_2){var _3;if(_2===_3)_2=this.getHiddenErrors();if(_1===_3)_1=this.getErrors();if(_1&&!this.showInlineErrors&&(!this.$52o||this.$52o.destroyed))
this.createErrorItem();this.markForRedraw("Validation Errors Changed");if(_1&&!isc.isAn.emptyObject(_1)){for(var _4 in _1){var _5=this.getItem(_4);if(_5&&_5.isVisible()&&_5.isDrawn()){this.$11b(_5);break}}}
if(!this.showInlineErrors){this.delayCall("scrollIntoView",[0,0],100)}
if(_2){this.$21z(_2)}}
,isc.A.getHiddenErrors=function(){if(!this.errors)return null;var _1=false,_2={};for(var _3 in this.errors){var _4=this.getItem(_3);if(!_4||!_4.visible){_1=true;_2[_3]=this.errors[_3]}}
return(_1?_2:null)}
,isc.A.showFieldErrors=function(_1){return this.showErrors()}
,isc.A.$21z=function(_1){if(_1==null||isc.isAn.emptyObject(_1))return;var _2;if(this.handleHiddenValidationErrors){_2=this.handleHiddenValidationErrors(_1)}
if(_2==false)return;var _3="Validation errors occurred for the following fields "+"with no visible form items:";for(var _4 in _1){var _5=_1[_4];if(!isc.isAn.Array(_5))_5=[_5];if(_5.length==0)continue;_3+="\n"+_4+":";for(var i=0;i<_5.length;i++){_3+=(i==0?"- ":"\n - ")+_5[i]}}
this.logWarn(_3,"validation")}
,isc.A.isRequired=function(_1){return(isc.DynamicForm.canEditField(_1,this)&&(_1.required||_1.$11m||this.isXMLRequired(_1)))}
,isc.A.setRequiredIf=function(){var _1=this.getValues();for(var _2=0;_2<this.items.length;_2++){var _3=this.items[_2],_4=_3.validators;if(!_3.visible||!_4||_4.length==0)continue;for(var v=0;v<_4.length;v++){var _6=_4[v];if(!_6)continue;if(_6.type==this.$10i){var _7=_3.getValue();if(_6.expression!=null&&!isc.isA.Function(_6.expression)){isc.Func.replaceWithMethod(_6,"expression","item,validator,value")}
_3.$11m=_6.expression.apply(this,[_3,_6,_7])}}}}
,isc.A.setFocusItem=function(_1){_1=this.getItem(_1);this.$10n=_1}
,isc.A.getFocusItem=function(){return this.$10n}
,isc.A.$vl=function(){return!this.isDisabled()}
,isc.A.setFocus=function(_1){if(!this.$vl())return;var _2=this.isVisible();if(_1){var _3=this.getFocusItem();if(_3==null){var _4=this.getItems();for(var i=0;i<_4.length;i++){var _6=_4[i];if(_6.$kk()&&_6.isDrawn()&&_6.isVisible()&&!_6.isDisabled())
{_3=_6;break}}}
var _7=isc.EH.lastEvent;if(!(_7.target==this&&_7.eventType==isc.EH.MOUSE_DOWN)){return this.focusInItem(_3)}}
this.Super("setFocus",arguments);if(!_1){this.$11r(this.getFocusItem())}}
,isc.A.$kf=function(_1,_2,_3){if(_3||!this.items||this.items.length==0||(_2&&isc.EH.targetIsMasked(this,_2)))
{return this.Super("$kf",arguments)}
var _4=this.items,_5=this.getFocusItem();if(_5==null){this.focusAtEnd(_1);return}
while(_5.parentItem){if(_5.$11s(_1))return;_5=_5.parentItem}
if(_5.$11s(_1))return;_5=this.$11t(_5,_1);if(_5!=null){this.focusInItem(_5)}else{if(isc.EH.$kj==this&&isc.EH.$kl==this){this.focusAtEnd(_1)}else{return this.Super("$kf",arguments)}}}
,isc.A.$11t=function(_1,_2){var _3=this.items,_4=_1,_5=_1.getGlobalTabIndex(),_6,_7,_8=_3.indexOf(_1);for(var i=0;i<_3.length;i++){if(_3[i]==_1)continue;var _10=_3[i].getGlobalTabIndex();if(_10<0){continue}
if(!this.$11u(_3[i],true))continue;if(_2){if(_10==_5&&i>_8){_6=_3[i];break}
if(_10>_5&&(_7==null||_7>_10))
{_6=_3[i];_7=_10}}else{if((_10<_5||(_10==_5&&_8>i))&&(_7==null||_7<=_10))
{_6=_3[i];_7=_10}}}
return _6}
,isc.A.focusAtEnd=function(_1){if(!this.items)return;var _2,_3,_4=this.items;for(var i=0;i<_4.length;i++){var _6=_4[i],_7=_6.getGlobalTabIndex();if(_7<0||!this.$11u(_6,true))continue;if((_3==null)||(_1&&_7<_3)||(!_1&&_7>=_3))
{_2=_6;_3=_7}}
if(_2&&this.$11u(_2,true))this.focusInItem(_2);else{var _8,_9=isc.EH.clickMaskRegistry;if(_9){for(var i=_9.length-1;i>=0;i--){if(isc.EH.isHardMask(_9[i])){_8=_9[i];break}}}
this.$kf(_1,_8,true)}}
,isc.A.$11u=function(_1,_2){if(isc.isA.String(_1))_1=this.getItem(_1);return _1&&_1.$kk()&&_1.isDrawn()&&_1.isVisible()&&!_1.isDisabled()&&(!_2||_1.tabIndex!=-1)}
,isc.A.focusInItem=function(_1){if(_1!=null){var _2=this.getItem(_1)}else{var _2=this.getFocusItem()}
if(!_2){this.logWarn("couldn't find focus item: "+_1);return}
if(_2.$kk()){_2.focusInItem();this.setFocusItem(_2);if(this.$10u){var _3=this;isc.Page.setEvent("idle",function(){if(!_3.destroyed)_3.focusInItem()},isc.Page.FIRE_ONCE)}}else{this.logWarn("focusInItem: item cannot accept focus: "+_2)}}
,isc.A.clearFocusItem=function(){delete this.$10n}
,isc.A.blurFocusItem=function(){var _1=this.getFocusItem();if(_1!=null){this.$11r(_1);this.clearFocusItem()}}
,isc.A.$11r=function(_1){if(_1!=null)_1.blurItem()}
,isc.A.$106=function(){var _1=this.getFocusItem();if(_1!=null&&_1.hasFocus){if(this.$11v==null)this.$11v=0;else this.$11v+=1;this.$11r(_1)}else{this.logDebug("blur w/o handler: no item to blur")}}
,isc.A.$11b=function(_1){if(!_1||!this.$11u(_1)){var _2;if(_1&&_1.parentItem){this.$11b(_1.parentItem);_2=true}
this.logInfo("$11b("+_1+"): not calling focus as item not focusable or item already has focus"+(_2?". Putting focus into containerItem instead.":""),"nativeFocus")
return}
var _3=_1.hasFocus;if(isc.Browser.isIE){var _4=isc.DynamicForm.$mu(document.activeElement);_3=(_4&&_4.item==_1)}
if(_3)return;this.$65n(_1);this.focusInItem(_1)}
,isc.A.$65n=function(_1){if(this.$11w==null)this.$11w=0;else this.$11w+=1;this.$11x=_1}
,isc.A.setOpacity=function(_1,_2,_3,_4,_5,_6){var _7=this.opacity;this.invokeSuper(isc.DynamicForm,"setOpacity",_1,_2,_3,_4,_5,_6);_1=this.opacity;if(isc.Browser.isMoz&&this.hasFocus&&(_1!=_7)&&(_1==null||_1==100||_7==null||_7==100))
{var _8=this.getFocusItem();if(_8&&_8.$429()){this.$106();this.$11b(_8)}}}
,isc.A.clearingElement=function(_1){if(this.$11w!=null&&this.$11x==_1){delete this.$11w;delete this.$11x}
if(this.$11v!=null&&(this.getFocusItem()==_1)){delete this.$11v}}
,isc.A.hide=function(){if(isc.Browser.isMoz)this.$11r(this.getFocusItem());this.Super("hide",arguments)}
,isc.A.setVisibility=function(_1,_2,_3,_4){this.invokeSuper(isc.DynamicForm,"setVisibility",_1,_2,_3,_4);this.itemsVisibilityChanged();if(this.isVisible()&&this.autoFocus)this.focus()}
,isc.A.clear=function(){this.Super("clear",arguments);this.itemsVisibilityChanged()
this.$11a()}
,isc.A.$lf=function(_1){this.Super("$lf",arguments);if(!this.hasFocus)this.$11r(this.getFocusItem())}
,isc.A.parentVisibilityChanged=function(_1){if(!this.isVisible()&&isc.Browser.isMoz)this.$11r(this.getFocusItem());this.Super("parentVisibilityChanged",arguments);this.itemsVisibilityChanged();if(this.isVisible()&&this.autoFocus)this.focus()}
,isc.A.$kr=function(_1){var _2=this.$ne(_1);if(_2.item){var _3=_2.item.$kr(_1,_2);if(_3!=null)return _3}
return this.Super("$kr",arguments)}
,isc.A.prepareForDragging=function(_1,_2,_3,_4){var _5=this.ns.EH;if(_5.dragTarget)return;var _6=_5.lastEvent,_7=this.$ne(_6);if(_7.item&&(_7.overElement||_7.overTextBox||_7.overControlTable))return false;return this.invokeSuper(isc.DynamicForm,"prepareForDragging",_1,_2,_3,_4)}
,isc.A.$ne=function(_1){if(!_1)_1=isc.EH.lastEvent;var _2=isc.EH.isMouseEvent(_1.eventType)?_1.nativeTarget:_1.nativeKeyTarget;var _3=isc.DynamicForm.$mu(_2,this);_1.itemInfo=_3;return _3}
,isc.A.handleMouseStillDown=function(_1,_2){var _3=this.$ne(_1),_4=((_3.overTitle||_3.inactiveContext)?null:_3.item);if(_4!=null&&_4.mouseStillDown){if(_4.handleMouseStillDown(_1)==false)return false}}
,isc.A.handleMouseDown=function(_1,_2){var _3=this.$ne(_1),_4=(_3.overTitle?null:_3.item);if(_4!=null){_4.handleMouseDown(_1);if(isc.Browser.isSafari&&!_3.inactiveContext&&_3.overElement&&isc.isA.CheckboxItem(_4))
{_4.focusInItem()}}}
,isc.A.$11y=function(_1,_2){var _3=this.$11z,_4=this.$110,_5=this.$111,_6=_1.item,_7=_1.overTitle,_8=_1.overIcon;if(_1.inactiveContext!=null){_6=null;_7=null;_8=null}
if(_3&&_3.destroyed){_3=null;this.$11z=null;this.$112=null;this.$110=null}
if(_6&&_6.destroyed){_6=null;_7=null;_8=null}
this.$11z=_6;this.$110=_7;this.$111=_8;if(_2==isc.EH.MOUSE_OVER){if(_6){if(_7)_6.handleTitleOver();else{if(_8)_6.$113(_8);_6.handleMouseOver()}}}else if(_2==isc.EH.MOUSE_OUT){if(_3){if(_4)_3.handleTitleOut();else{if(_5)_3.$114(_5);_3.handleMouseOut()}}}else{var _9=(_3!=_6||_4!=_7);if(_9){if(_3){if(_4)_3.handleTitleOut();else{if(_5)_3.$114(_5);_3.handleMouseOut()}}
if(_6){if(_7)_6.handleTitleOver();else{if(_8)_6.$113(_8);_6.handleMouseOver()}}}else{if(_7)_6.handleTitleMove();else{if(_5!=_8){if(_5)_6.$114(_5);if(_8)_6.$113(_8)}else if(_6){if(_8)_6.$115(_8);_6.handleMouseMove()}}}}}
,isc.A.handleMouseOver=function(_1,_2){if(this.mouseOver&&this.mouseOver(_1,_2)==false)return false;this.$11y(this.$ne(_1),isc.EH.MOUSE_OVER)}
,isc.A.handleMouseMove=function(_1,_2){if(this.mouseMove&&this.mouseMove(_1,_2)==false)return false;var _3=this.$ne(_1);this.$11y(_3,isc.EH.MOUSE_MOVE)}
,isc.A.handleMouseOut=function(_1,_2){this.$11y({},isc.EH.MOUSE_OUT);if(this.mouseOut&&this.mouseOut(_1,_2)==false)return false}
,isc.A.bubbleItemHandler=function(_1,_2,_3,_4,_5,_6){var _7=this.getItemById(_1),_8=null;for(;_7!=null;_7=_7.parentItem){if(_7[_2]!=null&&!isc.isA.Function(_7[_2])){isc.Func.replaceWithMethod(_7,_2,"arg1,arg2,arg3,arg4")}
if(_7[_2]==null){this.logWarn("handler:"+_2+" is not present on itemID "+_1);return false}
_8=_7[_2](_3,_4,_5,_6);if(_8==false)return _8}
return _8}
,isc.A.bubbleInactiveEditorEvent=function(_1,_2,_3){return this.bubbleItemHandler(_1,"$680",_2,_3.inactiveContext,_3)}
,isc.A.elementChanged=function(_1){var _2=this.bubbleItemHandler(_1,"elementChanged",_1);return(_2!=false)}
,isc.A.handleClick=function(_1,_2){var _3=this.$ne(_1);if(this.editingOn){if(!_3||!_3.item||(!_3.inactiveContext&&!_3.overTitle&&!_3.overIcon&&!_3.overElement&&!_3.overTextBox&&!_3.overControlTable)){var _4=false;if(_3&&_3.item&&_3.item.isA("SpacerItem")){_4=true}
this.logWarn("No item clicked upon, passing the click to the DF","EventHandler");if(!_4)return this.Super("handleClick",arguments)}}
if(_3&&_3.item){var _5=_3.item;if(_3.inactiveContext){this.logInfo("Bubbling inactive editor event for "+_5.ID,"EventHandler");return this.bubbleInactiveEditorEvent("click",_5,_3)}else{if(_3.overTitle){this.logInfo("Bubbling handleTitleClick event for "+_5.ID,"EventHandler");return this.bubbleItemHandler(_5,"handleTitleClick",_5)}else{var _6=(_3.overElement||_3.overTextBox||_3.overControlTable);_6=_6||_5.isA("SpacerItem");if(_3.overIcon){if(_5.$116(_3.overIcon)==false)
return false;var _7=_5.$117(_3.overIcon);if(_7&&_7.writeIntoItem)
_6=true}
if(_6)this.logInfo("Bubbling handleClick event for "+_5.ID,"EventHandler");if(_6&&this.bubbleItemHandler(_5,"handleClick",_5)==false){return false}
if(_6)this.logInfo("Bubbling handleCellClick event for "+_5.ID,"EventHandler");return this.bubbleItemHandler(_5,"handleCellClick",_5)}}}
return this.Super("handleClick",arguments)}
,isc.A.handleDoubleClick=function(_1,_2){var _3=this.$ne(_1);if(_3&&_3.item){var _4=_3.item;if(_3.inactiveContext){return this.bubbleInactiveEditorEvent(_4,"doubleClick",_3)}else if(_3.overTitle){return this.bubbleItemHandler(_4,"handleTitleDoubleClick",_4)}else{var _5=(_3.overElement||_3.overTextBox||_3.overControlTable);if(_3.overIcon){if(_4.$116(_3.overIcon)==false)return false;var _6=_4.$117(_3.overIcon);if(_6&&_6.writeIntoItem)_5=true}
if(_5&&this.bubbleItemHandler(_4,"handleDoubleClick",_4)==false){return false}
return this.bubbleItemHandler(_4,"handleCellDoubleClick",_4)}}}
,isc.A.elementFocus=function(_1,_2){if(!this.hasFocus)isc.EventHandler.focusInCanvas(this);var _3=this.getItemById(_2);this.setFocusItem(_3);var _4=true,_5=false;if(this.$11w!=null){if(this.$11x!=_3){delete this.$11w;delete this.$11x}else{_5=true;this.$11w-=1;if(this.$11w<0){delete this.$11w;delete this.$11x}}}
_4=this.bubbleItemHandler(_2,"elementFocus",_5);return(_4!=false)}
,isc.A.elementBlur=function(_1,_2){if(!isc.isA.FormItem(this.getItemById(_2)))return;var _3=true;if(this.$11v==null)_3=this.bubbleItemHandler(_2,"elementBlur");else{this.$11v-=1;if(this.$11v<0)delete this.$11v}
this.clearPrompt();return(_3!=false)}
,isc.A.handleKeyPress=function(_1,_2){if(_1.keyName==this.$10j){if(this.saveOnEnter){var _3=this.getFocusItem();if(isc.isA.TextItem(_3))this.submit()}}
if(_1.characterValue!=null&&_1.characterValue!=0&&(_1.keyName!="Escape"))
{return isc.EventHandler.STOP_BUBBLING}
if(!this.$118){this.$118={"Arrow_Up":true,"Arrow_Down":true,"Arrow_Right":true,"Arrow_Left":true,"Page_Up":true,"Page_Down":true,"Home":true,"End":true,"Backspace":true,"Delete":true,"Tab":true}}
if(this.$118[_1.keyName]&&_1.keyTarget!=this){return isc.EventHandler.STOP_BUBBLING}
return this.Super("handleKeyPress",arguments)}
,isc.A.itemHoverHTML=function(_1){if(_1.implementsPromptNatively)return null;var _2=_1.prompt;if(!_2&&_1.parentItem)_2=this.itemHoverHTML(_1.parentItem)
return _2}
,isc.A.titleHoverHTML=function(_1){if(_1.prompt)return _1.prompt;if(this.shouldClipTitle(_1))return _1.getTitle()}
,isc.A.$119=function(_1,_2){if(_2&&!isc.is.emptyString(_2)){var _3=this.$wc(_1);isc.Hover.show(_2,_3,(_1.hoverRect||this.itemHoverRect))}else isc.Hover.clear()}
,isc.A.$wc=function(_1){if(!isc.isA.FormItem(_1))_1=this.getItem(_1);return{width:(_1&&_1.hoverWidth!=null?_1.hoverWidth:this.itemHoverWidth),height:(_1&&_1.hoverHeight!=null?_1.hoverHeight:this.itemHoverHeight),align:(_1&&_1.hoverAlign!=null?_1.hoverAlign:this.itemHoverVAlign),baseStyle:(_1&&_1.hoverStyle!=null?_1.hoverStyle:this.itemHoverStyle),opacity:(_1&&_1.hoverOpacity!=null?_1.hoverOpacity:this.itemHoverOpacity),moveWithMouse:this.hoverMoveWithMouse}}
,isc.A.showPrompt=function(_1){window.status=_1}
,isc.A.clearPrompt=function(){window.status=""}
,isc.A.isMultipart=function(){return!(this.encoding==isc.DynamicForm.NORMAL||this.encoding==isc.DynamicForm.NORMAL_ENCODING)}
,isc.A.itemIsLastInRow=function(_1,_2){var _3=this.items.$8j,_4=_3[_2],_5=this.getItems().indexOf(_1);if(!_4||_5<0)return false;if(_4[this.numCols-1]==_5)return true;return false}
,isc.A.getColumnWidths=function(){var _1=this.items.$8j,_2=[];_2.length=this.numCols;for(var j=0;j<_2.length;j++)_2[j]=0;for(var _4=0;_4<_1.length;_4++){var _5=_1[_4];for(var i=0;i<_5.length;i++){var _7=this.items.get(_5[i]);if(_7.colSpan&&_7.colSpan>1)continue;if(_7.showTitle&&(this.titleOrientation=="left"||!this.titleOrientation)){if(_7.getVisibleTitleWidth()>_2[i]){_2[i]=_7.getVisibleTitleWidth()}
i++}
if(_7.width>_2[i])_2[i]=_7.width;if(_7.showTitle&&_7.titleOrientation=="right"&&_7.getVisibleTitleWidth()>_2[i+1]){_2[++i]=_7.getVisibleTitleWidth()}}}
return _2}
,isc.A.getItemTableOffsets=function(_1,_2){var _3=_2||this.items.$8j,_4=this.getItems().indexOf(_1),_5={};_5.itemIndex=_4
for(var _6=0;_6<_3.length;_6++){var _7=_3[_6],_8=_7.indexOf(_4),_9=_7.lastIndexOf(_4);if(_8>-1&&_9>-1){if(!_5.left||_8<_5.left)_5.left=_8;if(!_5.width||_5.width<_9-_8)_5.width=_9-_8+1;if(!_5.top||_6<_5.top)_5.top=_6;if(!_5.height||_5.height<_6-_5.top){_5.height=_6-_5.top+1}}}
return _5}
,isc.A.getItemDropIndex=function(_1,_2){if(!_1)return;if(!_2)_2=="L";var _3=this.getItemTableOffsets(_1),_4=this.items.$8j;if(_2=="L")return _3.itemIndex;if(_2=="R"){if(this.itemIsLastInRow(_1)&&this.canAddColumns!=true){return _3.itemIndex+1}
return _3.itemIndex+1}
if(_2=="T"){return this.getItemIndexAtTableLocation(_3.top-(_3.top==0?0:1),_3.left)}
if(_2=="B"){var _5=_3.top+_3.height-1;var _6=this.getItemIndexAtTableLocation(_5+1,_3.left);if(_6==null){_6=this.items.length}
return _6}}
,isc.A.getItemIndexAtTableLocation=function(_1,_2){var _3=this.items.$8j;if(!_3[_1])return;return _3[_1][_2]}
,isc.A.getItemAtPageOffset=function(_1,_2){this.items.$69e=this.getColumnWidths();var _3=this.items.$8j,_4=this.items.$69e,_5=this.items.$8s;var _6=this.inWhichPosition(_4,_1-this.getPageLeft()),_7=this.inWhichPosition(_5,_2-this.getPageTop());_6=_6==-1?0:_6==-2?_4.length:_6;_7=_7==-1?0:_7==-2?_5.length:_7;if(!_3[_7])return null;var _8=_3[_7][_6],_9=this.getItem(_8);if(_9!=null){_9.$69f=_7;_9.$69g=_6;_9.$69d=_8}
return _9}
,isc.A.getNearestItem=function(_1,_2){var _3=9999999999,_4;this.logDebug("Computing nearest item to ("+_1+","+_2+")","formItemDragDrop");for(var i=0;i<this.items.length;i++){var _6=this.items[i];var _7=_6.getPageRect(true),_8=_7[0],_9=_7[1],_10=_7[2],_11=_7[3],_12=0,_13=0;if(_1>=_8&&_1<=_8+_10&&_2>=_9&&_2<=_9+_11)
{return _6}
if(_1>_8){if(_1>_8+_10){_12=_1-(_8+_10)}}else{_12=_8-_1}
if(_2>_9){if(_2>_9+_11){_13=_2-(_9+_11)}}else{_13=_9-_2}
var _14=Math.sqrt(_12*_12+_13*_13);this.logDebug("Item "+_6.name+": (l,t,w,h) = "+_7,"formItemDragDrop");this.logDebug("XDelta: "+_12+", yDelta: "+_13+", straight line distance: "+_14,"formItemDragDrop");if(_14<_3){this.logDebug("Item "+_6.name+": distance is shorter than "+_3+", it is now the nearest item","formItemDragDrop");_3=_14;_4=_6}}
return _4}
,isc.A.showDragLineForItem=function(_1,_2,_3){this.makeDragLine();if(!_1){this._dragLine.hide();return}
var _4=_1.getPageRect(),_5=_4[0],_6=_4[1],_7=_4[2],_8=_1.getVisibleHeight(),_9=this.titleOrientation||"left";if(_1.showTitle!=false){if(_9=="left"||_9=="right")_7+=_1.getVisibleTitleWidth();if(_9=="left")_5-=_1.getVisibleTitleWidth()}
var _10;if(_2<=_5)_2=_5+1;else if(_2>=_5+_7){_2=_5+_7-1;_10=true}
var _11=_7/ 4;if(_11>20)_11=20;if(_3<=_6)_3=_6+1;else if(_3>=_6+_8)_3=_6+_8-1;var _12=_2-_5,_13=Math.round(_7/ _12),_14=_3-_6,_15=Math.round(_8/ _14),_16=(_5+_7)-_2,_17=Math.round(_7/ _16),_18=(_6+_8)-_3,_19=Math.round(_8/ _18),_20="R",_21,_22,_23,_24;_5--;_6--;if(_10||(Math.min(_13,_17)<Math.min(_15,_19)&&((_13>_17&&_12<_11)||(_17>_13&&_16<_11)))){_20=_10?"R":_13>_17?"L":"R";_22=3;_21=_8;_23=_20=="L"?_5:_5+_7-1;_24=_6}else{_20=_15>_19?"T":"B";_22=_7;_23=_5;_21=3;_24=_20=="T"?_6:_6+_8-1}
_1.dropSide=_20;if(this.itemIsLastInRow(_1,_1.$69f)&&!this.canAddColumns&&_1.dropSide=="R"){this.hideDragLine();this.setNoDropIndicator();this.$69h=this.currentCursor;this.setCursor("not-allowed")}
else{if(this.$uh){this.clearNoDropIndicator()
this.setCursor(this.$69h)}
var _25={left:_23,top:_24};this.adjustDragLinePosition(_25,_1,_20);_23=_25.left;_24=_25.top;this._dragLine.resizeTo(_22,_21);this._dragLine.setPageRect(_23,_24);this._dragLine.bringToFront();this._dragLine.show()}}
,isc.A.adjustDragLinePosition=function(_1,_2,_3){var _4=this.items.$8j,_5=this.items.indexOf(_2),_6,_7,_8;for(var i=0;i<_4.length;i++){if(_4[i].indexOf(_5)!=-1){_6=i;_7=_4[i].indexOf(_5);_8=_4[i].lastIndexOf(_5);break}}
if(_6==null||_7==null||_8==null)return;if(_3=="T"){if(_6==0)return;if(_4[_6-1][_7]==_4[_6-1][_8]&&_4[_6-1][_7-1]!=_4[_6-1][_7]&&_4[_6-1][_8+1]!=_4[_6-1][_7])
{var _10=this.items[_4[_6-1][_7]].getPageRect(true);var _11=_10[1]+_10[3];_1.top-=Math.round((_1.top-_11)/2)}}
if(_3=="B"){if(_6==_4.length-1)return;if(_4[_6+1][_7]==_4[_6+1][_8]&&_4[_6+1][_7-1]!=_4[_6+1][_7]&&_4[_6+1][_8+1]!=_4[_6+1][_7])
{var _10=this.items[_4[_6+1][_7]].getPageRect(true);var _11=_10[1];_1.top+=Math.round((_11-_1.top)/2)}}
if(_3=="L"){if(_7==0)return;var _10=this.items[_4[_6][_7-1]].getPageRect(true);var _12=_10[0]+_10[2];_1.left-=Math.round((_1.left-_12)/2)}
if(_3=="R"){if(_8==_4[_6].length-1)return;var _10=this.items[_4[_6][_8+1]].getPageRect(true);var _12=_10[0];_1.left+=Math.round((_12-_1.left)/2)}}
,isc.A.showDragLineForForm=function(){this.makeDragLine();this._dragLine.resizeTo(3,this.getHeight());this._dragLine.setPageRect(this.getPageLeft(),this.getPageTop());this._dragLine.bringToFront();this._dragLine.show()}
,isc.A.enableField=function(_1){if(_1==null||isc.isAn.emptyString(_1))return;var _2=this.getItem(_1);if(_2)_2.enable()}
,isc.A.disableField=function(_1){if(_1==null||isc.isAn.emptyString(_1))return;var _2=this.getItem(_1);if(_2)_2.disable()}
,isc.A.showField=function(_1){if(_1==null||isc.isAn.emptyString(_1))return;var _2=this.getItem(_1);if(_2)_2.show()}
,isc.A.hideField=function(_1){if(_1==null||isc.isAn.emptyString(_1))return;var _2=this.getItem(_1);if(_2)_2.hide()}
);isc.B._maxIndex=isc.C+216;isc.A=isc.DynamicForm;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.defaultFieldType="text";isc.A.$12a="link";isc.A.$gx="text";isc.A.$12b="select";isc.A.$12c="checkbox";isc.A.$12d="staticText";isc.A.$g2="boolean";isc.A.$12e="binary";isc.A.$12f="blob";isc.A.$52w="multifile";isc.A.$12g="multiupload";isc.A.$9i="upload";isc.A.$52x="file";isc.A.$51x="base64Binary";isc.A.$12h="enum";isc.A.$12i="CycleItem";isc.A.$12j="selectOther";isc.A.$12k="relation";isc.A.$67f="NestedEditorItem";isc.A.$67g="NestedListEditorItem";isc.A.$677="imageFile";isc.A.$678="ViewFileItem";isc.A.$450="id";isc.B.push(isc.A.getEditorType=function(_1,_2){if(_1._constructor==isc.FormItem.Class)_1._constructor=null;var _3=_2.getDataSource();var _4=this.defaultFieldType,_5=_1.editorType||_1.formItemType||_1._constructor||_1.type||_4;if(_1.editorType||_1.formItemType||_1._constructor)return _5;if(_5==this.$12a){if(!this.canEditField(_1,_2))_5=this.$12a;else _5=this.$gx}else if(!this.canEditField(_1,_2)){if(_5==this.$12e||_5==this.$52x||_5==this.$677)
_5=this.$678;else _5=this.$12d}else if(_5==this.$g2){var _6=_1.valueMap;if(!isc.isAn.Array(_6)&&isc.isAn.Object(_6))_5=this.$12b;else _5=this.$12c}else if(_5==this.$12e||_5==this.$12f||_5==this.$52x||_5==this.$677)
{if(_1.dataSource)_5=this.$52w
else _5=this.$52x}else if(_5==this.$12g){_5=this.$52w}else if(_5==this.$51x){_5=this.$51x}else if(_5==this.$12h){if(_1.showValueIconOnly)_5=this.$12i
else _5=this.$12b}else if(isc.DataSource&&isc.isA.DataSource(_3)&&_3.fieldIsComplexType(_1.name)){_5=_1.multiple?_2.nestedListEditorType:_2.nestedEditorType}else{if(!_1.type||(_1.type==_4)||(_1.type!=this.$12j&&(isc.FormItemFactory.getItemClass(_1.type)==null)))
{if(_1.dataSource){_5=this.$12k}else if(_1.valueMap||_1.optionDataSource||_1.displayField){_5=(_1.showValueIconOnly?this.$12i:this.$12b)}else if(_2&&(_1.length&&_1.length>_2.longTextEditorThreshold))
{_5=_2.longTextEditorType}else{_5=_4}}}
return _5}
,isc.A.canEditField=function(_1,_2){if(_1.canEdit!=null)return _1.canEdit;if(_2.canEdit!=null)return _2.canEdit;if(_1.canSave==false&&(!_2||!_2.$66m)){return false}
return true}
,isc.A.$mu=function(_1,_2){var _3=_2?_2.getClipHandle():document,_4={},_5=isc.DynamicForm.$89,_6=isc.DynamicForm.$9a,_7=isc.DynamicForm.$9b,_8=isc.DynamicForm.$9c,_9=isc.DynamicForm.$9e,_10=isc.DynamicForm.$9g;while(_1&&_1!=_3&&_1!=document){var _11=_1.getAttribute?_1.getAttribute(_5):null;if(_11!=null&&!isc.isAn.emptyString(_11)){var _12=window[_11];if(_12&&!_12.destroyed){_4.item=_12;var _13=_12.$681(_1);if(_13!=null){if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("Event occurred over inactive HTML for item:"+_12+" inactiveContext:"+this.echo(_13),"inactiveEditorHTML")}
_4.inactiveContext=_13}
var _14=_1.getAttribute(_6);if(_14==_7)_4.overElement=true;else if(_14==_10)_4.overTitle=true;else if(_14==_8)_4.overTextBox=true;else if(_14==_9)_4.overControlTable=true;else if(_14&&!isc.isAn.emptyString(_14))
_4.overIcon=_14;break}}
_1=_1.parentNode}
return _4}
,isc.A.getSimpleErrors=function(_1,_2){var _3={};if(isc.isAn.Array(_1))_1=_1[0];for(var _4 in _1){var _5=_1[_4];if(isc.isAn.Array(_5)){_3[_4]=[];for(var i=0;i<_5.length;i++){var _7=_5[i];_3[_4][i]=isc.isAn.Object(_7)?isc.shallowClone(_7):{errorMessage:_7}}}else{_3[_4]=isc.isAn.Object(_5)?isc.shallowClone(_5):{errorMessage:_5}}}
return _3}
,isc.A.formatValidationErrors=function(_1){var _2={};if(isc.isAn.Array(_1))_1=_1[0];for(var _3 in _1){var _4=_1[_3];if(isc.isAn.Array(_4)){_2[_3]=[];for(var i=0;i<_4.length;i++){var _6=_4[i];if(isc.isAn.Object(_6))_6=_6.errorMessage;_2[_3][i]=_6}}else{_2[_3]=isc.isAn.Object(_4)?_4.errorMessage:_4}}
return _2}
,isc.A.compareValues=function(_1,_2){if(_1==_2)return true;if(isc.isA.Date(_1)&&isc.isA.Date(_2))
return(Date.compareDates(_1,_2)==0);else if(isc.isAn.Array(_1)&&isc.isAn.Array(_2)){return _1.equals(_2)}else if(isc.isAn.Object(_1)&&isc.isAn.Object(_2)){var _3=isc.addProperties({},_2);for(var _4 in _1){if(_2[_4]!=_1[_4])return false;delete _3[_4]}
for(var _4 in _3){return false}
return true}
return false}
,isc.A.getFilterCriteria=function(){var _1={};for(var i=0;i<arguments.length;i++){var _3=arguments[i];if(_3==null)continue;isc.addProperties(_1,_3.getFilterCriteria())}
return _1}
);isc.B._maxIndex=isc.C+7;isc.defineClass("InlineFormItem","DynamicForm");isc.A=isc.InlineFormItem.getPrototype();isc.A.position="relative";isc.A.writeFormTag=false;isc.A.canSubmit=true;isc.A.numCols=1;isc.A.autoDraw=true;isc.A=isc.InlineFormItem;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.create=function(_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13){var _14=isc.addProperties({showTitle:false,validate:function(){this.form.validate()},destroy:function(){this.form.destroy()}},_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13);var _15=this.createRaw().completeCreation({fields:[_14],valuesManager:_14.valuesManager},_14.formProperties);return _15.getItem(0)}
);isc.B._maxIndex=isc.C+1;isc.A=isc.DynamicForm;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.makeInlineItem=function(_1,_2,_3,_4){return isc.InlineFormItem.create({name:_1,type:_2,formProperties:_4},_3)}
,isc.A.getFormValues=function(_1){return isc.Canvas.getFormValues(_1)}
);isc.B._maxIndex=isc.C+2;isc.DynamicForm.registerStringMethods({itemChanged:"item,newValue",itemChange:"item,newValue,oldValue",itemKeyPress:"item,keyName,characterValue",submitValues:"values,form",handleHiddenValidationErrors:"errors"});isc.ClassFactory.defineClass("FormItem");isc.A=isc.FormItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$68s=[null,"_inactiveContext",null];isc.A.$557=isc.Canvas.getPrototype().$557;isc.A.$63x=isc.Canvas.getPrototype().$63x;isc.A.reuseDOMIDs=false;isc.B.push(isc.A.$qs=function(_1,_2,_3,_4){if(_4==null&&this.isInactiveHTML()){_4=this.$68u}
if(_4!=null){this.$68s[0]=_1;this.$68s[2]=_4;_1=this.$68s.join(isc.emptyString);if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("$qs called for inactive HTML -- generated partName:"+_1,"inactiveEditorHTML")}
_2=false}
return isc.Canvas.getPrototype().$qs.apply(this,[_1,_2,_3])}
);isc.B._maxIndex=isc.C+1;isc.A=isc.FormItem;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.create=function(_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13){this.logWarn("Unsupported call to "+this.getClassName()+".create(). FormItems must be created "+"by their containing form. To create form items, use the 'items' property of a DynamicForm "+"instance. See documentation for more details.");return isc.addProperties({},_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13)}
,isc.A.getNewTagID=function(){if(this.$12m==null)this.$12m=0;this.$12m+=1;return"isc_FormItemElement_ID_"+this.$12m}
,isc.A.setElementTabIndex=function(_1,_2){_1.tabIndex=_2;if(isc.Browser.isMoz){_1.style.MozUserFocus=(_2<0?"ignore":"normal")}}
,isc.A.$12n=function(_1){if(!isc.Browser.isIE)return;var _2=this.getActiveElement();if(_2&&_2.tagName==null)_2=null;if(_2&&((_2.tagName.toLowerCase()==this.$12o&&_2.type.toLowerCase()==this.$12p)||_2.tagName.toLowerCase()==this.$12q))
{var _3=_2.createTextRange();_3.execCommand("Unselect")}}
,isc.A.$12r=function(_1,_2){if(isc.isA.FormItem(_1))_1=_1.getClassName();if(!this.$12s){this.$12s={text:true,TextItem:true,textItem:true,textArea:true,TextAreaItem:true,textAreaItem:true}
this.$12t={popUpTextArea:true,PopUpTextAreaItem:true,popUpTextAreaItem:true}}
return this.$12s[_1]||(!_2||this.$12t[_1])}
,isc.A.$12u=function(){if(!window.isc||!isc.DynamicForm)return;var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3&&_3.isDisabled()){_1.blur();return}
if(_3){return _3.$12v(_1,_3)}}
,isc.A.$12w=function(){if(!window.isc)return;var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3&&_3.hasFocus){return _3.$12x(_1,_3)}}
,isc.A.$43a=function(){if(!window.isc)return;var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3&&_3.hasFocus){return _3.$43a(_1,_3)}}
,isc.A.$12y=function(){if(!window.isc||!isc.DynamicForm)return;var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3)return _3.$12z()}
,isc.A.$120=function(){var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item,_4=_2.overIcon;if(_3){if(_3.iconIsDisabled(_4))_1.blur();else return _3.$121(_4,_1)}}
,isc.A.$122=function(){if(!window.isc)return;var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item,_4=_2.overIcon;if(_3&&!_3.iconIsDisabled(_4))return _3.$123(_4,_1)}
,isc.A.$124=function(){return false}
,isc.A.getErrorPromptString=function(_1,_2){var _3="";if(!isc.isAn.Array(_1))_1=[_1];for(var i=0;i<_1.length;i++){_3+=(i>0?(isc.Browser.isMoz?"  ":"\n"):"")+_1[i]};if(_2)_3=_3.replace(/'/g,"&#39;");return _3}
);isc.B._maxIndex=isc.C+13;isc.A=isc.FormItem;isc.A.$12o="input";isc.A.$12p="text";isc.A.$12q="textarea";isc.A=isc.FormItem.getPrototype();isc.A.emptyDisplayValue="";isc.A.fetchMissingValues=true;isc.A.valueIconSize=16;isc.A.valueIconLeftPadding=0;isc.A.valueIconRightPadding=3;isc.A.showFocusedPickerIcon=false;isc.A.pickerIconHSpace=0;isc.A.pickerIconSrc="[SKIN]/DynamicForm/pickerIcon.gif";isc.A.visible=true;isc.A.accessKey=null;isc.A.changeOnKeypress=true;isc.A.maintainSelectionOnTransform=true;isc.A.dirtyOnKeyDown=true;isc.A.showTitle=true;isc.A.width="*";isc.A.height=20;isc.A.colSpan=1;isc.A.rowSpan=1;isc.A.defaultIconSrc="[SKIN]/DynamicForm/default_formItem_icon.gif";isc.A.iconHSpace=3;isc.A.iconVAlign=isc.Canvas.BOTTOM;isc.A.iconHeight=20;isc.A.iconWidth=20;isc.A.iconPrompt="";isc.A.showIcons=true;isc.A.redrawOnShowIcon=true;isc.A.errorIconHeight=16;isc.A.errorIconWidth=16;isc.A.errorIconSrc="[SKIN]/DynamicForm/validation_error_icon.png";isc.A.showHint=true;isc.A.showFocused=false;isc.A.showDisabled=true;isc.A.cellStyle="formCell";isc.A.hintStyle="formHint";isc.A.titleStyle="formTitle";isc.A.$125=false;isc.A=isc.FormItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$o5="height";isc.A.$o6="width";isc.A.$126="colSpan";isc.A.$127="rowSpan";isc.A.$pa="*";isc.A.$bv="_";isc.A.$128="value";isc.A.$129="dataElement";isc.A.$o9="%";isc.A.errorMessageWidth=80;isc.A.$68w=1;isc.A.$682=new RegExp(".*_inactiveContext(.*)$");isc.A.$13a="<DIV STYLE='position:absolute;left:";isc.A.$13b="px;top:";isc.A.$13c="px;width:";isc.A.$13d="px;height:";isc.A.$13e="px;' ID='";isc.A.$13f="'>";isc.A.$13g="</DIV>";isc.A.$13h=["<SPAN style='white-space:nowrap;' eventProxy=",," "+isc.DynamicForm.$89+"='",,"' ID='",,"'>"];isc.A.$13i="</SPAN>";isc.A.$13j="$13k";isc.A.$13l="$13m";isc.A.$13n=["<DIV isDisabledEventMask='true' style='overflow:hidden;position:absolute;width:",null,"px;height:",null,"px' "+isc.DynamicForm.$89+"='",null,"' "+isc.DynamicForm.$9a+"='"+isc.DynamicForm.$9b+"' ID='",,"'>",(isc.Browser.isIE?isc.Canvas.spacerHTML(1600,100):"&nbsp;"),"</DIV>"];isc.A.$13o="hiddenDataElement";isc.A.$13p="control";isc.A.$13q="textBox";isc.A.$13r="pickerIconCell";isc.A.$59p="hintCell";isc.A.$13s="FormItemStyling";isc.A.$13t="deprecated";isc.A.$13u="Over";isc.A.$13v="Down";isc.A.$13w="Disabled";isc.A.$13x="valueIcon";isc.A.$13y=["<TABLE CELLSPACING=0 CELLPADDING=0 BORDER=0 ID='",,"' STYLE='"+isc.Canvas.$42a,,"' CLASS='",,"'><TR>",,"<TD style='",,"' VALIGN=",,">"];isc.A.$13z="</TD></TR></TABLE>";isc.A.$130=["<TABLE ID='",,"' "+isc.DynamicForm.$89+"='",,"' "+isc.DynamicForm.$9a+"='"+isc.DynamicForm.$9e,"' CELLPADDING=0 CELLSPACING=0 STYLE='",,"' CLASS='",,"'><TR><TD style='",,"'>",,"</TD><TD ID='",,"' CLASS='",,"' STYLE='",,"'>",,"</TD></TR></TABLE>"];isc.A.$131=["</TD><TD VALIGN=",," WIDTH=",," style='"+isc.Canvas.$42a+"' class='",,"'><NOBR>",,"</NOBR>"];isc.A.$132=["</TD><TD ID='",,"' CLASS='",,"'>"];isc.A.$133="$134";isc.A.$135="white-space:normal;";isc.A.$136="white-space:nowrap;";isc.A.$137="min-width:";isc.A.$138="min-height:";isc.A.$93="width:";isc.A.$92="height:";isc.A.$38="px;";isc.A.$39=";";isc.A.$139={};isc.A.$14a="cursor:default;";isc.A.textBoxCellCSS=isc.Canvas.$42a;isc.A.$14b="overflow:hidden;";isc.A.$14c="text-align:";isc.A.$14d="font-size:";isc.A.$14e=" ACCESSKEY='";isc.A.$14f=" TABINDEX='";isc.A.$ob="'";isc.A.$14g=["<DIV ID='",,"' "+isc.DynamicForm.$89+"='",,"' "+isc.DynamicForm.$9a+"='"+isc.DynamicForm.$9c,"' CLASS='",,"' STYLE='",,"'",,">",,,"</DIV>"];isc.A.$n3="drawing";isc.A.$14h="#";isc.A.$14i="vertical-align:";isc.A.$14j={};isc.A._$_iLink_="_iLink_";isc.A._$_iImg_="_iImg_";isc.A.$14k="cell";isc.A.applyStaticTypeFormat=true;isc.A.$14l="smart";isc.A.$14m={"Tab":true,"Arrow_Left":true,"Arrow_Right":true,"Arrow_Up":true,"Arrow_Down":true,"Home":true,"End":true,"Page_Up":true,"Page_Down":true,"Enter":true};isc.A.shouldSaveValue=true;isc.A.$14n="character";isc.A.$14o="EndToEnd";isc.A.$14p="EndToStart";isc.A.$14n="character";isc.A.$683={};isc.A.$115=isc.Class.NO_OP;isc.A.$10j="Enter";isc.A.$54n="img";isc.A.$jf="Tab";isc.A.$14q="$14r";isc.A.$14s={colSpan:true,rowSpan:true,startRow:true,endRow:true,showTitle:true,showHint:true};isc.A.$59q={baseStyle:true,showErrorStyle:true,showFocused:true,showErrorStyle:true,controlStyle:true,pickerIconStyle:true,textBoxStyle:true};isc.A.$59r="itemCellStyle";isc.B.push(isc.A.init=function(){if(isc.$cv)arguments.$cw=this;if(this.ID==null||window[this.ID]!=this){isc.ClassFactory.addGlobalID(this)}
if(this.options&&!this.valueMap){this.valueMap=this.options;delete this.options}
this.$14t(this.$o5);this.$14t(this.$o6);this.$14t(this.$126);this.$14t(this.$127);this._value=this.getDefaultValue();this.$14u=true;this.$14v();if((!this.validateOnExit||!this.synchronousValidation)&&this.validators&&this.validators.length>0)
{for(var i=0;i<this.validators.length;i++){if(this.validators[i].stopOnError){this.validateOnExit=true;this.synchronousValidation=true;break}}}
if((!this.validateOnExit||!this.synchronousValidation)&&((this.stopOnError==null&&this.form&&this.form.stopOnError)||this.stopOnError))
{this.validateOnExit=true;this.synchronousValidation=true}}
,isc.A.$14t=function(_1){var _2=this[_1];if(_2==null||isc.isA.Number(_2)||_2==this.$pa)return _2;var _3=parseInt(_2);if(_3==_2){this[_1]=_3;return _2}
return _2}
,isc.A.destroy=function(){if(this.isDrawn())this.cleared();var _1=this.pickList;this.pickList=null;if(_1!=null){if(_1.formItem==this)delete _1.formItem;if(_1.isVisible())_1.hide();if(!this.reusePickList())_1.destroy()}
this.destroyed=true;this.form=null;this.$14x=null;var _2;isc.ClassFactory.dereferenceGlobalID(this);this.$63x();if(isc.EH.$52d==this)isc.EH.$52d=null}
,isc.A.clear=function(){if(this.picker)this.picker.clear()}
,isc.A.toString=function(){var _1=this.getFieldName(),_2=this.ID,_3="["+this.Class+" instance "+(_1!=null?" name ='"+_1+"', ":"")+"global ID="+_2+"]";return _3}
,isc.A.getDataSource=function(){if(isc.isA.String(this.dataSource))return isc.DS.get(this.dataSource);return this.dataSource}
,isc.A.registerWithDataView=function(_1){if(!this.inputDataPath)return;if(!_1){_1=this.form;while(_1&&!isc.isA.DataView(_1))_1=_1.parentElement}
if(!_1){this.logWarn("Component initialized with an inputDataPath property, but no DataView "+"was found in the parent hierarchy. inputDataPath is only applicable to "+"DataBoundComponents and FormItems being managed by a DataView");return}
_1.registerItem(this)}
,isc.A.getFieldName=function(){return this.name}
,isc.A.getDataPath=function(){return this.dataPath}
,isc.A.getItemName=function(){return this.getFieldName()}
,isc.A.getElementName=function(){if(this.isInactiveHTML())return"";var _1=this.getFieldName();if(this.parentItem){var _2=this.parentItem.getElementName();if(_1==isc.emptyString)_1=_2;else _1=[_2,this.$bv,_1].join(isc.emptyString)}
if(_1==null||_1==this.getID()||_1==isc.emptyString){_1=this.$qs(this.$128)}
return _1}
,isc.A.getDataElementId=function(){if(this.isInactiveHTML())return this.$qs(this.$129);if(this.$14y==null){this.$14y=this.$qs(this.$129,true)}
return this.$14y}
,isc.A.getItemID=function(){return this.getID()}
,isc.A.getID=function(){if(this.ID==null){isc.ClassFactory.addGlobalID(this)}
return this.ID}
,isc.A.shouldShowTitle=function(){return this.showTitle}
,isc.A.getTitleHTML=function(){var _1=this.getTitle();if(!this.$kk())return _1;if(this.accessKey!=null){_1=isc.Canvas.hiliteCharacter(_1,this.accessKey)}
if(!this.hasDataElement())return _1;return isc.SB.concat("<LABEL FOR=",this.getDataElementId(),(this.accessKey!=null?" ACCESSKEY="+this.accessKey:isc.$ad),">",_1,"</LABEL>")}
,isc.A.getTitle=function(){if(this[this.form.titleField]!=null)return this[this.form.titleField];return this[this.form.fieldIdProperty]}
,isc.A.getTitleOrientation=function(){return this.form.getTitleOrientation(this)}
,isc.A.isVisible=function(){if(!this.containerWidget.isVisible())return false;if(this.visible==false)return false;if(this.parentItem&&!this.parentItem.isVisible())return false;return true}
,isc.A.getRowSpan=function(){return this.rowSpan}
,isc.A.getColSpan=function(){if(this.colSpan==0)this.colSpan=1;return this.colSpan}
,isc.A.isStartRow=function(){return this.startRow}
,isc.A.isEndRow=function(){return this.endRow}
,isc.A.getRect=function(){return[this.getLeft(),this.getTop(),this.getVisibleWidth(),this.getVisibleHeight()]}
,isc.A.getPageRect=function(_1){if(_1)return this.getPageRectIncludingTitle();return[this.getPageLeft(),this.getPageTop(),this.getVisibleWidth(),this.getVisibleHeight()]}
,isc.A.getPeerRect=function(){return this.getPageRect()}
,isc.A.getPageRectIncludingTitle=function(){var _1=this.getPageLeft(),_2=this.getPageTop(),_3=this.getVisibleWidth(),_4=this.getVisibleHeight();if(this.showTitle){var _5=this.getTitlePageLeft(),_6=this.getTitlePageTop(),_7=this.getVisibleTitleWidth(),_8=this.form.getTitleHeight(this);;if(this.titleOrientation=="left"||this.titleOrientation=="left"||this.titleOrientation==null)
{_1=_1<_5?_1:_5;_3+=_7}else{_1=_1<_5?_1:_5;_3=_3>_7?_3:_7;if(isc.isA.Number(_8))_4+=_8}}
return[_1,_2,_3,_4]}
,isc.A.getCellHeight=function(_1){if(isc.$cv)arguments.$cw=this;var _2=this.getHeight(_1);if(!isc.isA.Number(_2))return _2;var _3=this.getIconsHeight();if(_2<_3){_2=_3}
if(this.showPickerIcon&&this.pickerIconHeight){var _4=this.pickerIconHeight+this.$14z();if(_4>_2)_2=_4}
var _5=this.containerWidget;if(this.$11e()||!isc.isA.DynamicForm(_5))return _2;_2+=this.$140();if(this.showTitle&&this.form.getTitleOrientation(this)==isc.Canvas.TOP){_2+=this.form.getTitleHeight(this)}
return _2}
,isc.A.$140=function(){var _1=0,_2=this.form,_3=this.getCellStyle();if(this.parentItem)_2=this.parentItem;_1+=2*_2.cellSpacing;var _4=isc.isA.Number(_2.cellPadding)?_2.cellPadding:0,_5=isc.Element.$tt(_3,true);if(_5==null)_5=_4
var _6=isc.Element.$tu(_3,true);if(_6==null)_6=_4;_1+=_5;_1+=_6;_1+=isc.Element.$ym(_3);return _1}
,isc.A.$141=function(){var _1=0,_2=this.form,_3=this.getCellStyle();if(this.parentItem)_2=this.parentItem;if(isc.isA.Number(_2.cellSpacing))_1+=2*_2.cellSpacing;var _4=isc.isA.Number(_2.cellPadding)?_2.cellPadding:0,_5=isc.Element.$tr(_3,true);if(_5==null)_5=_4;var _6=isc.Element.$ts(_3,true);if(_6==null)_6=_4;_1+=_5;_1+=_6;_1+=isc.Element.$yn(_3);return _1}
,isc.A.getInnerHeight=function(){var _1=this.containerWidget;if(this.$11e())return this.$11p(this.height,true);if(this.$8t){var _2=this.$8t[1];if(!isc.isA.Number(_2))return _2;if(this.$142()){_2-=this.$140()}
return _2}
return this.getHeight()}
,isc.A.getInnerWidth=function(_1){var _2=this.containerWidget;if(this.$11e())return this.$11p(this.width);var _3=this.$8t?this.$8t[0]:this.width;if(!isc.isA.Number(_3))return _3;if(this.$142()){_3-=this.$141()}
return _3}
,isc.A.getColWidth=function(){var _1=this.form?this.form.items:null;if(_1&&_1.$8r!=null&&this.$8m!=null){var _2=this.$8m[0],_3=this.$8m[2];if(this.showTitle){var _4=this.getTitleOrientation();if(_4==isc.Canvas.LEFT)_2+=1;else if(_4==isc.Canvas.RIGHT)_3-=1}
var _5=0;for(var c=_2;c<_3;c++){_5+=_1.$8r[c]}
return _5}
return null}
,isc.A.$11e=function(){return(this.containerWidget.$11e&&this.containerWidget.$11e())}
,isc.A.$142=function(){return(this.containerItem!=null||(this.form==this.containerWidget&&!this.$11e()))}
,isc.A.$11p=function(_1,_2){if(isc.isA.String(_1)&&isc.endsWith(_1,this.$o9)){var _3=this.containerWidget,_4=_2?_3.getInnerHeight():_3.getInnerWidth();return Math.round((parseInt(_1,10)/100)*_4)}
return _1}
,isc.A.getElementWidth=function(){var _1=this.getInnerWidth();if(!isc.isA.Number(_1))return null;_1-=this.getTotalIconsWidth();return(isc.isA.Number(_1)?Math.max(_1,1):null)}
,isc.A.getTextBoxWidth=function(_1){var _2=this.getElementWidth();if(!isc.isA.Number(_2))return _2;if(this.textBoxStyle){var _3=this.getTextBoxStyle();_2-=(isc.Element.$tf(_3)+isc.Element.$tg(_3));if(this.$143()){_2-=isc.Element.$yp(_3)}}
if(this.showPickerIcon){_2-=this.getPickerIconWidth();var _4=this.getPickerIcon();if(_4.hspace!=null)_2-=_4.hspace;if(this.pickerIconStyle)
_2-=isc.Element.$yp(this.getPickerIconStyle());if(this.controlStyle)
_2-=isc.Element.$yp(this.getControlStyle())}
if(this.hasDataElement()&&this.$144(_1)){_2-=((this.getValueIconWidth()||0)+(this.valueIconLeftPadding+this.valueIconRightPadding))}
return _2-this.$672()}
,isc.A.getErrorWidth=function(){var _1=0;if(this.form.showInlineErrors&&this.hasErrors()){var _2=this.getErrorOrientation();if(_2==isc.Canvas.LEFT||_2==isc.Canvas.RIGHT){if(this.shouldShowErrorText()){_1+=this.errorMessageWidth}else if(this.shouldShowErrorIcon()){_1+=this.errorIconWidth+this.iconHSpace}}}
return _1}
,isc.A.$672=function(){var _1=this.getErrorWidth();if(_1!=0&&this.expandHintAndErrors&&(this.getColWidth()!=null)){var _2=this.getColWidth()-this.getInnerWidth();if(_2>0)_1-=_2;if(_1<0)_1=0}
return _1}
,isc.A.getValueIconHeight=function(){var _1=this.valueIconHeight;if(_1==null)_1=this.valueIconSize;return _1}
,isc.A.getValueIconWidth=function(){var _1=this.valueIconWidth;if(_1==null)_1=this.valueIconSize;return _1}
,isc.A.getTextBoxHeight=function(){var _1=(isc.isA.Number(this.getHeight())?this.getHeight():this.getInnerHeight());if(!isc.isA.Number(_1))return _1;if(this.valueIcons!=null||this.getValueIcon!=null){var _2=this.getValueIconHeight();if(_2>_1)_1=_2}
if(this.textBoxStyle){var _3=this.getTextBoxStyle();_1-=(isc.Element.$th(_3)+isc.Element.$ti(_3));if(this.$143()){_1-=isc.Element.$yo(_3)}}
if(this.showPickerIcon&&this.controlStyle){_1-=isc.Element.$yo(this.getControlStyle())}
if(this.showTitle&&this.form.getTitleOrientation(this)==isc.Canvas.TOP&&!isc.isA.Number(this.getCellHeight()))
{_1-=this.form.getTitleHeight(this)}
return _1}
,isc.A.$143=function(){return!isc.Browser.isBorderBox}
,isc.A.getPickerIconWidth=function(){return(this.pickerIconWidth!=null?this.pickerIconWidth:this.getPickerIconHeight())}
,isc.A.getPickerIconHeight=function(){if(this.pickerIconHeight!=null)return this.pickerIconHeight;else{var _1=(isc.isA.Number(this.getHeight())?this.getHeight():this.getInnerHeight());if(!isc.isA.Number(_1))return null;_1-=this.$14z();this.pickerIconHeight=_1;return _1}}
,isc.A.$14z=function(){var _1=0;if(this.controlStyle){_1+=isc.Element.$yo(this.controlStyle)}
if(this.pickerIconStyle){_1+=isc.Element.$yo(this.pickerIconStyle)}
return _1}
,isc.A.getHeight=function(){return this.height}
,isc.A.getVisibleHeight=function(){var _1=this.isDrawn()?this.getOuterElement():null;if(_1==null){this.logInfo("getVisibleHeight() - unable to determine drawn height for this item -"+" returning pixel height from specified height","sizing");if(isc.isA.Number(this.height)){return this.height}
this.logWarn("getVisibleHeight() unable to determine height - returning zero","sizing");return 0}
return _1.offsetHeight}
,isc.A.getIconHeight=function(_1){if(_1==null&&this.icons!=null&&this.icons.getLength()>0)_1=this.icons[0];else if(!this.$145(_1)){this.logWarn("getIconHeight() passed invalid icon:"+isc.Log.echoAll(_1));return null}
return(_1.height!=null?_1.height:this.iconHeight)}
,isc.A.getTitleVisibleHeight=function(){var _1=this.isDrawn()&&this.form?isc.Element.get(this.form.$427(this)):null;if(_1==null){var _2="getTitleHeight() Unable to determine position for "+(this.name==null?"this item ":this.name)+". ";if(this.isDrawn()){_2+="This method is not supported by items of type "+this.getClass()}else{_2+="Position cannot be determined before the element is drawn"}
_2+=" - returning zero.";this.form.logWarn(_2);return 0}
return isc.Element.getVisibleHeight(_1)}
,isc.A.getWidth=function(){return this.width}
,isc.A.getVisibleWidth=function(){var _1=this.isDrawn()?this.getOuterElement():null;if(_1==null){this.logInfo("getVisibleWidth() - unable to determine drawn width for this item -"+" returning pixel width from specified width","sizing");if(isc.isA.Number(this.width)){return this.width}else if(this.form&&this.form.items.$8r!=null){return this.form.items.$8r[this.form.getItems().indexOf(this)]}
this.logWarn("getVisibleWidth() unable to determine width - returning zero","sizing");return 0}
return _1.offsetWidth}
,isc.A.getVisibleTitleWidth=function(){var _1=this.isDrawn()&&this.form?isc.Element.get(this.form.$427(this)):null;if(_1==null){this.logInfo("getVisibleTitleWidth() - unable to determine drawn width for this "+"item - returning 0","sizing");return 0}
return _1.offsetWidth}
,isc.A.getIconWidth=function(_1){if(_1==null&&this.icons!=null&&this.icons.getLength()>0)_1=this.icons[0];else if(!this.$145(_1)){this.logWarn("getIconWidth() passed invalid icon:"+isc.Log.echoAll(_1));return null}
return(_1.width!=null?_1.width:this.iconWidth)}
,isc.A.setHeight=function(_1){this.height=_1;this.redraw()}
,isc.A.setWidth=function(_1){this.width=_1;this.redraw()}
,isc.A.setLeft=function(_1){this.left=_1;this.redraw()}
,isc.A.setTop=function(_1){this.top=_1;this.redraw()}
,isc.A.moved=function(){}
,isc.A.visibilityChanged=function(){}
,isc.A.zIndexChanged=function(){}
,isc.A.getInactiveEditorHTML=function(_1,_2,_3,_4){this.$68x=true;this.$68u=this.setupInactiveContext(_4);if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("getInactiveEditorHTML() called - context passed in:"+this.echo(_4)+" generated context ID:"+this.$68u,"inactiveEditorHTML")}
var _5=this.getStandaloneItemHTML(_1,_2,_3);delete this.$68u;delete this.$68x;return _5}
,isc.A.setupInactiveContext=function(_1){if(_1==null)_1={};if(this.containerWidget&&this.containerWidget.isPrinting)_1.isPrintHTML=true;var _2=this.$68w++;_1.inactiveContextID=_2;_1.formItem=this;if(!this.$68v)this.$68v={};this.$68v[_2]=_1;return _2}
,isc.A.clearAllInactiveEditorContexts=function(){delete this.$68v}
,isc.A.clearInactiveEditorContext=function(_1){if(isc.isAn.Object(_1))_1=_1.inactiveContextID;if(this.$68v)delete this.$68v[_1]}
,isc.A.$681=function(_1){if(_1&&_1.id!=null&&this.$68v!=null){var _2=_1.id,_3=this.$557(_2);if(_3){var _4=_3.match(this.$682);if(_4){return this.$68v[_4[1]]}}}
return null}
,isc.A.isInactiveHTML=function(){if(this.parentItem&&this.parentItem.isInactiveHTML())return true;return this.$68y()||this.$68x}
,isc.A.$68y=function(){return this.containerWidget&&this.containerWidget.isPrinting}
,isc.A.getStandaloneItemHTML=function(_1,_2,_3){var _4=isc.SB.create(),_5=this.form;if(_5){if(this.$11e()){var _6=this.$11p(this.left),_7=this.$11p(this.top,true),_8=this.getInnerWidth(),_9=this.getInnerHeight();if(!isc.isA.Number(_6))_6=0;if(!isc.isA.Number(_7))_7=0;_4.append(this.$13a);_4.appendNumber(_6);_4.append(this.$13b);_4.appendNumber(_7);if(isc.isA.Number(_8)){_4.append(this.$13c);_4.appendNumber(_8)}
if(isc.isA.Number(_9)){_4.append(this.$13d);_4.appendNumber(_9)}
_4.append(this.$13e,this.$146(),this.$13f)}
var _10=this.$13h,_11=_5.getID(),_12=this.getID();_10[1]=_11;_10[3]=_12;_10[5]=this.$qs(this.$13j);_4.append(_10);_4.append(this.getInnerHTML(_1,_2,_3,true));_4.append(this.$13i);if(this.$11e()){_4.append(this.$13g)}}
return _4.release()}
,isc.A.$146=function(){return this.$qs(this.$13l)}
,isc.A.getAbsDiv=function(){if(this.$13m)return this.$13m;if(!this.isDrawn())return;this.$13m=isc.Element.get(this.$146());return this.$13m}
,isc.A.$147=function(){var _1=this.icons;if(!_1)return false;for(var i=0;i<_1.length;i++){if(!_1[i].writeIntoItem)return true}
return false}
,isc.A.useDisabledEventMask=function(){return(isc.Browser.isMoz&&this.hasDataElement())||(isc.Browser.isIE&&isc.isA.TextItem(this))}
,isc.A.$148=function(){var _1=this.$13n;_1[1]=this.$149();_1[3]=this.getHeight();_1[5]=this.getItemID();_1[7]=this.$qs("eventMask");return _1.join(isc.emptyString)}
,isc.A.$43b=function(){return isc.Element.get(this.$qs("eventMask"))}
,isc.A.$149=function(){return this.getElementWidth()}
,isc.A.getBrowserSpellCheck=function(){if(this.browserSpellCheck!=null)return this.browserSpellCheck;return this.form.browserSpellCheck}
,isc.A.$11d=function(){return(this.shouldSaveValue&&!this.hasDataElement()&&this.shouldSubmitValue())}
,isc.A.$15a=function(){return this.$qs(this.$13o)}
,isc.A.$15b=function(){return this.$15c(this.$13o)}
,isc.A.$15c=function(_1){if(!this.isDrawn())return null;if(!this.$15d)this.$15d={};var _2=this.$15d[_1];if(_2==null){_2=isc.Element.get(this.$qs(_1));if(_2!=null)this.$15d[_1]=_2}
return _2}
,isc.A.$15e=function(){return this.$qs(this.$13p)}
,isc.A.$15f=function(){return this.$15c(this.$13p)}
,isc.A.$15g=function(){return this.$qs(this.$13q)}
,isc.A.$15h=function(){if(this.hasDataElement()&&this.$15i)return this.getDataElement();return this.$15c(this.$13q)}
,isc.A.$15j=function(){return this.$qs(this.$13r)}
,isc.A.$15k=function(){return this.$15c(this.$13r)}
,isc.A.$15l=function(){return"<INPUT type='hidden' name='"+this.getFieldName()+"' ID='"+this.$15a()+"'>"}
,isc.A.$59s=function(){return this.$qs(this.$59p)}
,isc.A.$59t=function(){return this.$15c(this.$59p)}
,isc.A.updateState=function(){if(!this.isDrawn())return;var _1=this.logIsDebugEnabled(this.$13s);if(this.containerWidget==this.form&&!this.$11e()){var _2=this.getCellStyle();if(_1)this.logDebug("About to apply basic cell style:"+_2,"FormItemStyling");var _3=this.getFormCell();if(_3)_3.className=_2;var _4=this.getOuterTableElement();if(_4)_4.className=_2;if(this.showTitle)this.form.updateTitleCellState(this)}
if(this.showPickerIcon){var _5=this.getControlStyle(),_6=this.getPickerIconStyle();if(_1){this.logDebug("About to apply cell styles to control box and picker icon cell:"+[_5,_6],"FormItemStyling")}
var _7=this.$15f();if(_7)_7.className=_5;var _8=this.$15k();if(_8)_8.className=_6}
var _9=this.getTextBoxStyle();if(_1)this.logDebug("About to apply text box style:"+_9,"FormItemStyling");var _10=this.$15h();if(_10){_10.className=_9}
if(this.$15m()&&_10){if(!this.$15n){var _11=this.getTextBoxWidth(),_12=this.getTextBoxHeight();_11+=isc.Element.getHBorderSize(_10)-2;_12+=isc.Element.getVBorderSize(_10)-2;var _13=this.$qs("focusOutline");isc.Element.insertAdjacentHTML(_10,"beforeBegin","<DIV ID='"+_13+(this.textBoxStyle?"' CLASS='"+this.textBoxStyle+"Focused'":"'")+" STYLE='background-image:none;background-color:transparent;position:absolute;width:"+_11+"px;height:"+_12+"px;visibility:hidden;border:1px dotted white;z-index:100;'>&nbsp;</DIV>");this.$15n=isc.Element.get(_13)}
if(this.hasFocus)this.$15n.style.visibility="inherit";else this.$15n.style.visibility="hidden"}}
,isc.A.$15o=function(_1,_2,_3){if(!this.logIsInfoEnabled(this.$13t))return;if(!this.$15p)this.$15p={};if(this.$15p[_1])return;if(_3==null)_3="5.5";var _4=isc.SB.create();_4.append("Using '",_1,"': ",this[_1]," to style this form item.  This property is deprecated as of SmartClient Version ",_3," - we recommend removing this property and using '",_2,"' instead.");this.logInfo(_4.release(),"deprecated");this.$15p[_1]=true}
,isc.A.getInnerHTML=function(_1,_2,_3,_4){var _5,_6;if(this.isInactiveHTML()&&this.$68u==null){_5=true;var _7,_8=this.parentItem;if(_8!=null&&_8.isInactiveHTML()){if(_8.$68u==null){_8.setupInactiveContext();_6=true}
_7=_8.$68v[_8.$68u]}
this.$68u=this.setupInactiveContext(_7);if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("getInnerHTML(): Item is marked as inactive - set up "+"new inactive context ID:"+this.$68u,"inactiveEditorHTML")}}
this.$15q=_2&&!this.$712();var _9;if((this.isInactiveHTML()||this.isDisabled())&&this.useDisabledEventMask()){_9=isc.SB.create();_9.append(this.$148())}
if(this.$11d()){if(!_9)_9=isc.SB.create();_9.append(this.$15l())}
if(this.$712())_2=false;var _10=this.$15r(_1,_2,_3);var _11;if(_9!=null){_9.append(_10);if(_4)_11=_9.getArray();_11=_9.release()}else{_11=(_4?_10:_10.join(isc.emptyString))}
if(_5)delete this.$68u;if(this.parentItem&&_6)
delete this.parentItem.$68u;return _11}
,isc.A.$15s=function(_1,_2){if(_2)return true;if(_1&&this.getHint()!=null)return true;if(this.icons&&this.icons.length>0)return true}
,isc.A.$144=function(_1){if(this.suppressValueIcon)return null;var _2,_3;if(_1===_3)_1=this.getValue();if(this.getValueIcon)_2=this.getValueIcon(_1);else{if(_1==null)_2=this.emptyValueIcon;else if(this.valueIcons!=null)_2=this.valueIcons[_1]}
if(_2==null)return null;var _4=(this.isDisabled()&&this.showValueIconDisabled?this.$13w:this.$15t);if(_4!=null){if(!isc.CheckboxItem.$15u)isc.CheckboxItem.$15u={};var _5=isc.CheckboxItem.$15u[_2];if(!_5){_5={};_5.Over=isc.Img.urlForState(_2,false,false,this.$13u);_5.Down=isc.Img.urlForState(_2,false,false,this.$13v);_5.Disabled=isc.Img.urlForState(_2,false,false,this.$13w);isc.CheckboxItem.$15u[_2]=_5}
_2=_5[_4]}
return _2}
,isc.A.$xq=function(_1){var _2=this.$144(_1);if(_2==null){return isc.emptyString}
var _3=this.imageURLPrefix||this.baseURL||this.imgDir,_4=this.imageURLSuffix;if(_4)_2=_2+_4;var _5=this.getValueIconWidth();var _6=this.getValueIconHeight();return isc.Canvas.$xq(_2,_3,_5,_6,this.valueIconLeftPadding,this.valueIconRightPadding,this.$qs(this.$13x))}
,isc.A.$15v=function(){if(!this.isDrawn())return null;var _1=isc.Element.get(this.$qs(this.$13x));return _1}
,isc.A.$15r=function(_1,_2,_3){var _4=this.getErrorOrientation(),_5,_6=_4==isc.Canvas.LEFT,_7;if(_3&&(_6||_4==isc.Canvas.RIGHT))
{var _8=this.getErrors();if(_8){_5=true;_7=this.getErrorHTML(_8)}}
var _9=this.iconVAlign,_10=this.mapValueToDisplay(_1),_11=this.$15s(_2,_5),_12=this.showPickerIcon;;var _13=_11?this.$13y:[];if(_11){_13.length=13;_13[1]=this.$15w();_13[3]=this.getOuterTableCSS();if(this.containerWidget==this.form&&!this.$11e()){_13[5]=this.getCellStyle()}else{_13[5]=null}
if(_5&&_6){_13[7]=isc.StringBuffer.concat("<TD STYLE='",isc.Canvas.$42a,"' CLASS='",this.getCellStyle(),"'>",_7,"</TD>")}else _13[7]=null;if(!_12)_13[9]=this.getTextBoxCellCSS();else _13[9]=isc.Canvas.$42a;_13[11]=_9}
if(!_12){_13[_13.length]=this.getElementHTML(_10,_1)}else{var _14=this.getPickerIconStyle(),_15=this.getID(),_16=this.getControlStyle(),_17=this.$130,_18=this.$15e(),_19=this.$15g(),_20=this.$15j();_17[1]=_18;_17[3]=_15;_17[6]=this.getControlTableCSS();if(_16==null&&this.containerWidget==this.form&&!this.$11e()){_17[8]=this.getCellStyle();_17[6]+=isc.Canvas.$42a}else{_17[8]=_16}
_17[10]=this.getTextBoxCellCSS();_17[12]=this.getElementHTML(_10,_1);_17[14]=_20;_17[16]=_14;_17[18]=this.getPickerIconCellCSS();var _21=this.getPickerIcon(),_22=_21&&this.hasFocus&&this.showFocusedPickerIcon&&(_21.showFocusedWithItem!=false);_17[20]=this.getIconHTML(this.getPickerIcon(),_22);for(var i=0;i<_17.length;i++){_13[_13.length]=_17[i]}}
if(_11){if(this.$147()){var _24=this.$131;_24[1]=_9;_24[3]=this.getTotalIconsWidth();_24[5]=this.getCellStyle();_24[7]=this.getIconsHTML();for(var i=0;i<_24.length;i++){_13[_13.length]=_24[i]}}
var _25=(_5&&!_6);var _26;if(_2){_26=this.getHint();if(isc.isA.emptyString(_26))_26=null}
if(_26||_25){var _27=this.$132;_27[1]=this.$59s();_27[3]=_26?this.getHintStyle():null;_27[5]=(_26||"")+(_25?_7||"":"");for(var i=0;i<_27.length;i++){_13[_13.length]=this.$132[i]}}
_13[_13.length]=this.$13z}
return _13}
,isc.A.$15w=function(){return this.$qs(this.$133)}
,isc.A.$15x=function(_1){var _2=this.hasErrors();if(!isc.FormItem.$15y)isc.FormItem.$15y={};var _3=isc.FormItem.$15y[_1];if(!_3){_3={};_3.Error=_1+"Error";_3.Focused=_1+"Focused"
_3.Disabled=_1+"Disabled"
isc.FormItem.$15y[_1]=_3}
if(_2){return(this.shouldShowErrorStyle()&&this.form.showInlineErrors?_3.Error:_1)}else{if(this.showFocused&&this.hasFocus&&!this.isInactiveHTML())
return _3.Focused;if(this.showDisabled&&this.isDisabled())return _3.Disabled;return _1}}
,isc.A.getCellStyle=function(){if(this.parentItem!=null){if(this.parentItem.itemCellStyle)return this.$15x(this.parentItem.itemCellStyle)}
var _1=this.$15x(this.cellStyle);if(!this.hasErrors()){if(this.cellClassName!=null){this.$15o("cellClassName","cellStyle");_1=this.cellClassName}}else{if(this.errorCellClassName!=null){this.$15o("errorCellClassname","cellStyle");_1=this.errorCellClassName}}
return _1}
,isc.A.getTitleStyle=function(){if(this.$68y()&&this.printTitleStyle){return this.$15x(this.printTitleStyle)}
var _1=this.getErrors();if(_1==isc.emptyString)_1=null;var _2=this.$15x(this.titleStyle);if(!_1){if(this.titleClassName!=null){this.$15o("titleClassName","titleStyle");_2=this.titleClassName}}else{if(this.titleErrorClassName!=null){this.$15o("titleErrorClassName","titleStyle");_2=this.titleErrorClassName}}
return _2}
,isc.A.getHintStyle=function(){if(this.hintClassName!=null){this.$15o("hintClassName","hintStyle");return this.hintClassName}
if(this.hintStyle!=null)return this.hintStyle}
,isc.A.getTextBoxStyle=function(){if(this.$68y()&&this.printTextBoxStyle){return this.$15x(this.printTextBoxStyle)}
var _1=this.textBoxStyle?this.$15x(this.textBoxStyle):null;if(this.elementClassName!=null){this.$15o("elementClassName","textBoxStyle");_1=this.elementClassName}
return _1}
,isc.A.getPickerIconStyle=function(){if(this.pickerIconStyle!=null)return this.$15x(this.pickerIconStyle);return null}
,isc.A.getControlStyle=function(){if(this.controlStyle!=null)return this.$15x(this.controlStyle);return null}
,isc.A.getOuterTableCSS=function(){var _1=this.$139;var _2,_3=this.expandHintAndErrors;if(_3&&this.getHint()==null){var _4=this.getErrorOrientation();_3=(_4==isc.Canvas.LEFT||_4==isc.Canvas.RIGHT)}
if(_3&&(this.getColWidth()!=null)){_2=Math.max(this.getInnerWidth(),this.getColWidth())}else{_2=this.getInnerWidth()}
if(!isc.isA.Number(_2)){if(!this.$15z)
this.$15z=this.$135;return this.$15z}else if(_1[_2]!=null){return _1[_2]}
var _5=isc.SB.create();_5.append(this.$135);_5.append(this.$93);_5.appendNumber(_2,5);_5.append(isc.semi);_1[_2]=_5.release();return _1[_2]}
,isc.A.getControlTableCSS=function(){var _1=isc.SB.create();_1.append(this.$14a);var _2=this.getElementWidth()-this.$672();if(isc.isA.Number(_2))_1.append(this.$93,_2,this.$38);return _1.release()}
,isc.A.getTextBoxCellCSS=function(){return this.textBoxCellCSS}
,isc.A.getTextBoxCSS=function(){var _1=isc.SB.create();var _2=this.getTextBoxWidth();if(isc.isA.Number(_2)){if((isc.Browser.isOpera||isc.Browser.isMoz||isc.Browser.isSafari)&&!this.clipValue){_1.append(this.$137,_2,this.$38)}else{_1.append(this.$93,_2,this.$38)}}
var _3=this.getTextBoxHeight();if(isc.isA.Number(_3)){if(isc.Browser.isMoz&&!this.clipValue){_1.append(this.$138,_3,this.$38)}else{_1.append(this.$92,_3,this.$38)}}
if(this.clipValue)_1.append(this.$14b);if(this.wrap)_1.append(this.$135)
else _1.append(this.$136);if(this.textAlign!=null){_1.append(this.$14c,this.textAlign,this.$39)}
return _1.release()}
,isc.A.getPickerIconCellCSS=function(){if(isc.Browser.isIE)return isc.emptyString;var _1=this.getPickerIconHeight();if(isc.isA.Number(_1)&&_1<this.getInnerHeight()){return this.$14d+_1+this.$38}
return isc.emptyString}
,isc.A.getPickerIcon=function(){if(this.$150==null){var _1=this.getPickerIconWidth(),_2=this.getPickerIconHeight();var _3={pickerIcon:true,writeIntoItem:true,showOver:this.showOver,showFocused:this.showFocusedPickerIcon,hspace:this.pickerIconHSpace,width:_1,height:_2,src:this.pickerIconSrc,click:function(_4,_5,_6){_5.showPicker()}};isc.addProperties(_3,this.pickerIconDefaults,this.pickerIconProperties);if(_3.$151==null)this.$152(_3);this.$150=_3;if(this.iconIsDisabled(_3))_3.$153=true}
return this.$150}
,isc.A.getElementHTML=function(_1,_2){var _3=isc.SB.create(),_4=this.$15m();var _5=this.$kk(),_6,_7;if(_5){var _8=this.$154(),_9=this.isDisabled()?null:this.accessKey;if(_4){_7=isc.Canvas.getFocusProxyString(this.getID(),false,0,0,this.getTextBoxWidth(),this.getTextBoxHeight(),this.isVisible(),!this.isDisabled(),_8,_9,false)}else{var _10=isc.SB.create();if(_9!=null)_10.append(this.$14e,_9,this.$ob);_10.append(this.$14f,_8,this.$ob);_6=_10.release()}}
if(_7!=null)_3.append(_7);var _11=this.$14g;_11[1]=this.$15g();_11[3]=this.getID();_11[6]=this.getTextBoxStyle();_11[8]=this.getTextBoxCSS();_11[10]=_6;_11[12]=this.$xq(_2);_11[13]=(this.showValueIconOnly?null:_1);_3.append(_11);return _3.release()}
,isc.A.$15m=function(){return(isc.Browser.isSafari||(isc.Browser.isMoz&&isc.Browser.geckoVersion<20051111))&&this.$kk()&&!this.hasDataElement()}
,isc.A.$155=function(){if(!isc.FormItem.$156){isc.FormItem.$156=[" ",isc.DynamicForm.$89,"='",null,"' ",isc.DynamicForm.$9a,"='",isc.DynamicForm.$9b,"'"]}
isc.FormItem.$156[3]=this.getItemID();return isc.FormItem.$156.join(isc.emptyString)}
,isc.A.getErrors=function(){if(this.form)return this.form.getFieldErrors(this)}
,isc.A.getError=function(){this.logWarn("call to deprecated method FormItem.getError()."+" Use FormItem.getErrors() instead.");return this.getErrors()}
,isc.A.getErrorMessage=function(_1){return(isc.isAn.Array(_1)?"<UL><LI>"+_1.join("</LI><LI>")+"</LI></UL>":_1)}
,isc.A.shouldShowErrorIcon=function(){return this.showErrorIcon!=null?this.showErrorIcon:this.form.showErrorIcons}
,isc.A.shouldShowErrorText=function(){return this.showErrorText!=null?this.showErrorText:this.form.showErrorText}
,isc.A.shouldShowErrorStyle=function(){return this.showErrorStyle!=null?this.showErrorStyle:this.form.showErrorStyle}
,isc.A.shouldShowErrorIconPrompt=function(){return this.shouldShowErrorIcon&&!this.shouldShowErrorText()}
,isc.A.getErrorOrientation=function(){return this.errorOrientation!=null?this.errorOrientation:this.form.errorOrientation}
,isc.A.getErrorHTML=function(_1){var _2=this.shouldShowErrorText(),_3=this.shouldShowErrorIcon();if(!_2&&!_3)return isc.emptyString;var _4=this.form,_5=_3&&_2,_6=!_5&&_3&&((this.getErrorOrientation()==isc.Canvas.LEFT)||(this.getErrorOrientation()==isc.Canvas.RIGHT)),_7=(_2&&this.form.showTitlesWithErrorMessages&&this.getTitle()!=null?this.getTitle()+": ":null),_8,_9=_2?this.getErrorMessage(_1):null;if(!_5){_8=isc.SB.concat("<DIV ",(_6?"style='display:inline;'":null)," CLASS='",this.getCellStyle(),"'>",(_3?this.getErrorIconHTML(_1)+"&nbsp;":null),_7,_9,"</DIV>")}else{_8=isc.SB.concat("<TABLE WIDTH=100% CELLSPACING=0 CELLPADDING=0><TR>","<TD WIDTH=",this.errorIconWidth,">",this.getErrorIconHTML(_1),"</TD><TD STYLE='",isc.Canvas.$42a,"' CLASS='",this.getCellStyle(),"'>&nbsp;",_7,_9,"</TD></TR></TABLE>")}
return _8}
,isc.A.getErrorIconHTML=function(_1){var _2=this.getErrorIconId();var _3=this.shouldShowErrorIconPrompt()?isc.FormItem.getErrorPromptString(_1,true):isc.emptyString;return this.$157(_2,this.errorIconWidth,this.errorIconHeight,"top",0,null,null,this.form.getImgURL(this.errorIconSrc),this.getID(),_2,null,true," title='"+_3+"'")}
);isc.evalBoundary;isc.B.push(isc.A.getPromptString=function(_1){}
,isc.A.getErrorIconId=function(){return this.$qs("error")}
,isc.A.getHint=function(){if(!this.showHint||!this.hint)return null
return this.hint}
,isc.A.drawn=function(){if(this.logIsInfoEnabled(this.$n3)){this.logInfo("Form item drawn "+(this.containerWidget==this.form?"in form "+this.form.getID():"in container widget "+this.containerWidget.getID())+(this.logIsDebugEnabled("drawing")?this.getStackTrace():""),"drawing")}
this.$if=true;if(this.$15q)this.$158=true;this.$15q=null;this.$159();if(isc.screenReader)this.addContentRoles()}
,isc.A.redrawing=function(){if(this.$16j(true)){this.$516()}
this.form.clearingElement(this);this.$13m=null}
,isc.A.redrawn=function(){if(this.logIsInfoEnabled("drawing")){this.logInfo("Form item redrawn "+(this.containerWidget==this.form?"in form "+this.form.getID():"in container widget "+this.containerWidget.getID())+(this.logIsDebugEnabled("drawing")?this.getStackTrace():""),"drawing")}
this.$16a();this.$159();if(isc.screenReader)this.addContentRoles();if(this.$16j(true)){if(isc.Browser.isIE){this.delayCall("$109",[],100)}else{this.$109()}}}
,isc.A.$516=function(){this.$105=true;this.rememberSelection();if(this.items){for(var i=0;i<this.items.length;i++){if(this.items[i].hasFocus){return this.items[i].$516()}}}
var _2=this.$166();if(_2!=null&&_2!=this.getFocusElement()){var _3=this.getPickerIcon();if(_3!=null&&this.$16g(_3)==_2){this.$65o=_3}else if(this.icons){for(var i=0;i<this.icons.length;i++){if(this.$16g(this.icons[i])==_2){this.$65o=this.icons[i];break}}}}}
,isc.A.$109=function(){var _1=this.isDrawn()&&this.isVisible();if(_1){var _2=isc.EH.getFocusCanvas();if(_2!=null&&_2!=this.form){_1=false}else{var _3=this.form.getFocusItem();if(_3!=this&&_3!=this.parentItem&&(!this.items||!this.items.contains(_3)))
{_1=false}}}
delete this.$105;if(_1)this.resetToLastSelection();if(this.items){for(var i=0;i<this.items.length;i++){if(this.items[i].$16j()){return this.items[i].$109()}}}
if(_1)this.form.$65n(this);if(this.$65o){var _5=this.$117(this.$65o);delete this.$65o;if(_5){if(_1){this.focusInIcon(_5)}
return}}
if(_1)this.focusInItem()}
,isc.A.$159=function(){if(this.$kk()){var _1=this.getFocusElement();if(!_1){this.logWarn("Attempting to apply event handlers to this item. "+"Unable to get a pointer to this item's focus element");return}
_1.onfocus=isc.FormItem.$12u;_1.onblur=isc.FormItem.$12w;if(isc.Browser.isIE){_1.onpaste=isc.FormItem.$43a;_1.oncut=isc.FormItem.$43a}
if(this.$16b){for(var _2 in this.$16b){_1[_2]=this.$16b[_2]}}}
this.$16c()}
,isc.A.$16c=function(){if(this.showPickerIcon)this.$16d(this.getPickerIcon());if(this.showIcons&&this.icons&&this.icons.length>0){for(var i=0;i<this.icons.length;i++){var _2=this.icons[i];if(_2&&(this.$16e(_2)||this.$16f(_2)))
this.$16d(_2)}}}
,isc.A.$16d=function(_1){if(!_1.imgOnly){var _2=this.$16g(_1);if(_2){_2.onfocus=isc.FormItem.$120
_2.onblur=isc.FormItem.$122
_2.href=this.$14h;_2.onclick=isc.FormItem.$124;if(isc.Browser.isSafari){if(!isc.FormItem.$16h){isc.FormItem.$16h=function(){var _3=isc.DynamicForm.$mu(this),_4=_3.item,_5=_3.overIcon;if(_4)return _4.focusInIcon(_5)}}
_2.onmousedown=isc.FormItem.$16h}}}}
,isc.A.cleared=function(){if(this.logIsInfoEnabled("drawing")){this.logInfo("Form item cleared "+(this.containerWidget==this.form?"from within form "+this.form.getID():"from within container widget "+this.containerWidget.getID())+(this.logIsDebugEnabled("drawing")?this.getStackTrace():""),"drawing")}
this.form.clearingElement(this);this.$16a();this.$158=false;this.$15q=false;this.$if=false}
,isc.A.$16a=function(){this.$14x=null;this.$13m=null;this.$16i=null;this.$15d={}}
,isc.A.isDrawn=function(){return this.$if}
,isc.A.$14v=function(){var _1=this.icons;if(_1==null)return;for(var i=0;i<_1.length;i++){var _3=_1[i];this.$36c(_3)}}
,isc.A.$36c=function(_1){if(_1.$151==null)this.$152(_1);if(this.iconIsDisabled(_1))_1.$153=true}
,isc.A.getIconsHTML=function(){if(!this.showIcons||this.icons==null)return"";var _1=this.$16j(true);if(this.showIconsOnFocus&&!_1){this.hideAllIcons();return""}
var _2=isc.SB.create();for(var i=0;i<this.icons.length;i++){var _4=this.icons[i];if(!this.$16f(_4)||this.$16e(_4))continue;var _5=_1&&this.$54o(_4,true);_2.append(this.getIconHTML(_4,null,this.isDisabled(),!!_5))}
return _2.release()}
,isc.A.$16j=function(_1){var _2=this.hasFocus||this.$105;if(_1&&!_2&&this.items!=null){for(var i=0;i<this.items.length;i++){if(this.items[i].hasFocus||this.items[i].$105)_2=true;break}}
return _2}
,isc.A.$152=function(_1){if(_1.$151!=null)return _1;if(this.$16k==null)this.$16k=0;_1.$151="_"+this.$16k++;return _1}
,isc.A.$16l=function(_1){if(this.$150&&(_1==this.$150))return null;var _2=this.iconVAlign;if(_2==isc.Canvas.TOP){return"top"}else if(_2==isc.Canvas.BOTTOM){return(isc.Browser.isSafari?"bottom":"text-bottom")}else if(_2==isc.Canvas.CENTER){return"middle"}
return _2}
,isc.A.$16m=function(){return 0}
,isc.A.getIconPrompt=function(_1){return _1.prompt||this.iconPrompt}
,isc.A.getIconURL=function(_1,_2,_3,_4){var _5=_1.src||this.defaultIconSrc,_6=(_3||this.iconIsDisabled(_1))?isc.StatefulCanvas.STATE_DISABLED:_2?isc.StatefulCanvas.STATE_OVER:null;_5=isc.Img.urlForState(_5,false,_4,_6);return _5}
,isc.A.getIconHTML=function(_1,_2,_3,_4){var _5=this.getIconURL(_1,_2,_3,_4),_6=this.getIconWidth(_1),_7=this.getIconHeight(_1),_8=(_1.hspace!=null?_1.hspace:this.iconHSpace),_9=_1.backgroundColor,_10=this.form.getID(),_11=this.getItemID(),_12=_1.$151;if(_1.imgOnly){return this.$157(this.$16n(_12),_6,_7,this.$16l(_1),this.$16m(_1),_8,_9,_5,_11,_12)}else{if(isc.FormItem.$16o==null){isc.FormItem.$16o=["<a ID='",,"'"," style='font-size:1px;margin-left:",,"px;"+(isc.Browser.isMoz?"-moz-user-focus:":""),,"' tabIndex=",," ",isc.DynamicForm.$89,"='",,"' ",isc.DynamicForm.$9a,"='",,"' handleNativeEvents=false>",,"</a>"]}
var _13=isc.FormItem.$16o;var _14=(this.iconIsDisabled(_1)||this.canTabToIcons==false)?-1:this.$16p(_1);_13[1]=this.$16q(_12);var _15=this.$16r(_1);if(_15)_13[4]=_8;else _13[4]="0"
if(isc.Browser.isMoz)_13[6]=(_14<0?"ignore":"normal");_13[8]=_14;_13[12]=_11;_13[16]=_12;_13[18]=this.$157(this.$16n(_12),_6,_7,this.$16l(_1),this.$16m(_1),(!_15?_8:null),_9,_5,_11,_12,_8);return _13.join(isc.emptyString)}}
,isc.A.$16r=function(_1){return(!isc.Browser.isIE&&!_1.imgOnly&&!isc.Browser.isStrict)}
,isc.A.$157=function(_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13){if(isc.FormItem.$16s==null){isc.FormItem.$16s=["ID='",,"' style='",,,";margin-top:",,"px;margin-bottom:",,"px;",,,,"'"]}
var _14=isc.FormItem.$16s;_14[1]=_1
if(_4!=null){_14[3]=this.$14i;_14[4]=_4}else{_14[3]=null;_14[4]=null}
_14[6]=_5;_14[8]=_5;if(_6!=null){_14[10]="margin-left:"+_6+"px;"}else{_14[10]=null}
_14[11]=(_7!=null?"background-color:"+_7+";":null);if(isc.Browser.isStrict&&!isc.Browser.isTransitional&&!_12)
_14[12]="display:block;"
else _14[12]=null;if(_13)_14[14]=_13;var _13=_14.join(isc.emptyString);_14.length=14;var _15=isc.FormItem.$4s=isc.FormItem.$4s||{align:isc.Browser.isSafari?"absmiddle":"TEXTTOP"};_15.src=_8;_15.width=_2;_15.height=_3;_15.extraStuff=_13;return isc.Canvas.imgHTML(_15)}
,isc.A.$16q=function(_1){if(this.isInactiveHTML()){return this.$qs(this._$_iLink_+_1)}
if(!this.$16t)this.$16t={};var _2=this.$16t;if(!_2[_1]){_2[_1]=this.$qs(this._$_iLink_+_1,true)}
return _2[_1]}
,isc.A.$16n=function(_1){if(this.isInactiveHTML()){return this.$qs(this._$_iImg_+_1)}
if(!this.$16u)this.$16u={};var _2=this.$16u;if(!_2[_1]){_2[_1]=this.$qs(this._$_iImg_+_1,true)}
return _2[_1]}
,isc.A.$16g=function(_1){_1=this.$117(_1);if(_1==null||_1.imgOnly)return null;var _2=this.$16q(_1.$151);return isc.Element.get(_2)}
,isc.A.$16v=function(_1){_1=this.$117(_1);if(_1==null)return null;var _2=this.$16n(_1.$151);return isc.Element.get(_2)}
,isc.A.$16w=function(_1){if(!_1||!this.icons)return null;var _2=isc.DynamicForm.$mu(_1);if(!_2||_2.item!=this)return null;return _2.icon}
,isc.A.$16f=function(_1){if(_1.showIf==null)return true;isc.Func.replaceWithMethod(_1,"showIf","form,item");return!!_1.showIf(this.form,this)}
,isc.A.$16e=function(_1){if(_1.writeIntoItem)return true;return false}
,isc.A.$16x=function(){if(!this.showIcons||this.icons==null||(this.showIconsOnFocus&&!this.hasFocus))return false;return true}
,isc.A.getTotalIconsWidth=function(){if(!this.$16x())return 0;var _1=0;for(var i=0;i<this.icons.length;i++){var _3=this.icons[i];if(!this.$16f(_3)||this.$16e(_3))continue;_1+=(_3.width!=null?_3.width:this.iconWidth)+(_3.hspace!=null?_3.hspace:this.iconHSpace)}
return _1}
,isc.A.getIconsHeight=function(){if(!this.$16x())return 0;var _1=0;for(var i=0;i<this.icons.length;i++){var _3=this.icons[i];if(!this.$16f(_3)||this.$16e(_3))continue;var _4=(_3.height!=null?_3.height:this.iconHeight);_4+=this.$16m()*2;if(_4>_1)_1=_4}
return _1}
,isc.A.setIcons=function(_1){this.icons=_1;this.$14v();this.redraw()}
,isc.A.addIcon=function(_1){if(!this.icons)this.icons=[];this.icons.add(_1);this.setIcons(this.icons);return _1}
,isc.A.getIconByProperty=function(_1,_2){if(this.icons)return this.icons.find(_1,_2)}
,isc.A.setIconEnabled=function(_1){_1=this.$117(_1);if(!_1)return;var _2=!this.iconIsDisabled(_1);if(!!_1.$153!=_2)return;if(!_2)_1.$153=true;else delete _1.$153;if(!this.isDrawn())return;var _3=this.$16g(_1),_4=this.$16v(_1);if(_3){if(!_2)isc.FormItem.setElementTabIndex(_3,-1);else isc.FormItem.setElementTabIndex(_3,this.$16p(_1))}
if(_4){var _5=this.getIconURL(_1,null,!_2);isc.Canvas.$wg(_4,_5)}}
,isc.A.showIcon=function(_1,_2){delete this.$16y;if(!isc.isAn.Object(_1))return;if(_1.$151==null){this.$152(_1)}
var _3=this.$16f(_1);_1.showIf=function(){return true}
if(!_3&&this.showIcons&&this.containerWidget.isDrawn()&&this.isVisible())
{if(this.redrawOnShowIcon||_1.writeIntoItem){this.redraw()}else{var _4;for(var i=(this.icons.indexOf(_1)-1);i>=0;i--){var _6=this.icons[i];if(!_6.writeIntoItem&&this.$16f(_6)){_4=_6;break}}
var _7=true;if(_4!=null){var _8;_8=_4.imgOnly?this.$16v(_4):this.$16g(_4);if(_8!=null){isc.Element.insertAdjacentHTML(_8,"afterEnd",this.getIconHTML(_1,null,this.isDisabled(),_2));this.$16z();_7=false}}
if(_7){this.logInfo("showIcon(): Unable to dynamically update icon visibility - "+"redrawing the form");return this.redraw()}else{this.$16d(_1)}}}}
,isc.A.hideIcon=function(_1){if(!isc.isAn.Object(_1))return;var _2=this.$16f(_1);_1.showIf=function(){return false}
if(_2&&this.showIcons&&this.containerWidget.isDrawn()&&this.isVisible())
{if(this.redrawOnShowIcon||_1.writeIntoItem){this.redraw()}
else{var _3=_1.imgOnly?this.$16v(_1):this.$16g(_1);if(_3==null){this.logInfo("hideIcon(): Unable to dynamically update icon visibility - "+"redrawing the form");return this.redraw()}
isc.Element.clear(_3);this.$16z()}}}
,isc.A.$16z=function(){this.$160()}
,isc.A.showAllIcons=function(_1){if(this.$161!=null){isc.Timer.clear(this.$161);delete this.$161}
this.$162(this.icons,_1)}
,isc.A.hideAllIcons=function(){if(this.$161!=null)delete this.$161;this.$163(this.icons);this.$16y=true}
,isc.A.$162=function(_1,_2){if(_1==null||_1.length==0)return;for(var i=0;i<_1.length;i++){_2=_2&&this.$54o(_1[i],true);this.showIcon(_1[i],_2)}}
,isc.A.$163=function(_1){if(_1==null||_1.length==0)return;for(var i=0;i<_1.length;i++){this.hideIcon(_1[i])}}
,isc.A.$117=function(_1){var _2;if(this.icons){for(var i=0;i<this.icons.length;i++){if(this.icons[i]==_1||this.icons[i].$151==_1)_2=this.icons[i]}}
if(!_2&&this.showPickerIcon){var _4=this.getPickerIcon();if(_4.id==_1||_4.id==_1.id)_2=_4}
if(!_2){this.logInfo("FormItem unable to get pointer to icon with ID:"+_1+" - Invalid ID, or icons array has been inappropriately modified."+" To update icon[s] for some form item, use the method 'setIcons()'.")}
return _2}
,isc.A.$164=function(_1,_2,_3){if(this.isDisabled())return;if(_3==null)_3=this.hasFocus&&this.$54o(_1,true)
var _4=this.$16v(_1);if(_4!=null){var _5=this.getIconURL(_1,_2,null,_3);isc.Canvas.$wg(_4,_5)}}
,isc.A.$165=function(_1){if(this.isDisabled())return false;if(_1.showOver!=null)return _1.showOver;return this.showOverIcons}
,isc.A.$54o=function(_1,_2){if(!_1||this.isDisabled())return false;if(_2&&_1.showFocusedWithItem==false)return false;if(_1.showFocused!=null)return _1.showFocused;return this.showFocusedIcons}
,isc.A.setIconBackgroundColor=function(_1,_2){_1.backgroundColor=_2;var _3=this.$16v(_1);if(_3!=null){try{_3.style.backgroundColor=_2}catch(e){}}}
,isc.A.showPicker=function(_1,_2,_3,_4){var _5=this.picker;_3=isc.addProperties(_3||{},{callingForm:this.form,callingFormItem:this});if(isc.isA.String(_5)&&isc.isA.Canvas(window[_5])){_5=this.picker=window[_5]}
if(!_5){_5=this.picker=this.createPicker(_3);if(!isc.isA.Function(_5.dataChanged)){_5.dataChanged=new Function()}
_5.observe(_5,"resized","observed.placeNear(observed.lastShowRect)");if(this.pickerDataChanged&&_5.dataChanged){this.observe(_5,"dataChanged","observer.pickerDataChanged(observed)")}}else{isc.addProperties(_5,_3)}
var _6=_5.getID();if(!_4){if(this.getPickerRect){_4=this.getPickerRect()}else if(_2){var _7=this.getIconPageRect(_2);_4=[_7[0],_7[1]]}
else _4=[isc.EH.getX(),isc.EH.getY()]}
_5.lastShowRect=_4;_5.setRect(_4);if(!_5.isDrawn()){_5.moveTo(null,-9999);_5.draw()}
this.picker.placeNear(_4);if(isc.isA.Function(_5.setData)){if(isc.isA.Function(this.getPickerData)){_5.setData(this.getPickerData(_5))}else _5.setData(this.getValue(_5))}
var _8=_1?null:_6+".hide()";if(_1&&isc.isA.Function(_5.clickMaskClicked))
_8=_6+".clickMaskClicked()";_5.showClickMask(_8,!_1,_5);if(_1!=null&&_5.isModal==null)_5.isModal=_1;_5.show();_5.bringToFront();_5.focus();return false}
,isc.A.createPicker=function(_1){return this.createAutoChild("picker",_1)}
,isc.A.hidePicker=function(){if(!this.picker)return;this.picker.hideClickMask();this.picker.hide()}
,isc.A.redraw=function(_1){if(!this.isDrawn())return;if(this.hasFocus)this.$105=true;if(!this.hasFocus&&this.items!=null){for(var i=0;i<this.items.length;i++){if(this.items[i].hasFocus)this.$105=true}}
if(this.containerWidget.redrawFormItem){this.containerWidget.redrawFormItem(this,_1)}else{this.containerWidget.markForRedraw("Form item redrawn"+(_1?": "+_1:isc.emptyString))}}
,isc.A.adjustOverflow=function(_1){if(!this.$43c){this.$43c=[this.getID(),"  overflow changed: "]}
if(_1==null)this.$43c[2]="No Reason Specified.";else this.$43c[2]=_1;if(isc.isA.DynamicForm(this.containerWidget))
this.containerWidget.adjustOverflow(this.$43c.join(isc.emptyString))}
,isc.A.show=function(_1){this.visible=true;if(!_1)this.showIf=null;if(this.containerWidget.redrawFormItem)this.containerWidget.redrawFormItem(this,"showing form item");else this.containerWidget.markForRedraw("showing form item");this.visibilityChanged(true)}
,isc.A.hide=function(_1){this.visible=false;if(!_1)this.showIf=null;if(this.containerWidget.redrawFormItem)this.containerWidget.redrawFormItem(this,"hiding form item");else this.containerWidget.markForRedraw("hiding form item");this.visibilityChanged(true)}
,isc.A.$104=function(){if(!isc.Browser.isSafari||!this.isDrawn())return;this.$160()}
,isc.A.$160=function(){if(!this.isDrawn())return;var _1=this.clipValue;var _2=this.getOuterTableElement();if(_2)_2.style.width=this.getInnerWidth();if(this.showPickerIcon){var _3=this.$15f();if(_3)_3.style.width=this.getElementWidth();var _4=this.getPickerIcon(),_5=this.$16v(_4);if(_5){_5.style.height=this.getPickerIconHeight();_5.style.width=this.getPickerIconWidth()}}
var _6=this.getTextBoxWidth(),_7=this.getTextBoxHeight(),_8=this.$15h();if(_8){if(_1)_8.style.width=_6;else _8.style.minWidth=_6;_8.style.height=_7}
if(this.$15m()){var _9=this.getFocusElement()
if(_9){_9.style.width=_6;_9.style.height=_7}}}
,isc.A.hasElement=function(){return this.hasDataElement()}
,isc.A.hasDataElement=function(){return this.$125}
,isc.A.getElement=function(_1){return this.getDataElement(_1)}
,isc.A.getFocusElement=function(){if(!this.isDrawn()||!this.$kk())return null;if(this.hasDataElement())return this.getDataElement();if(this.$15m()){if(!this.$16i){this.$16i=isc.Element.get(this.getID()+"__focusProxy")}
return this.$16i}
return this.$15h()}
,isc.A.$166=function(){if(this.hasFocus==null&&!isc.EH.$vm==this){return null}
var _1=this.$167;if(isc.Browser.isIE&&_1!=this.getActiveElement()){this.logInfo("not returning focus element since it doesn't match "+"document.activeElement","nativeFocus");if(this.hasFocus){this.elementBlur()}
this.$167=null;return null}
return _1}
,isc.A.getDataElement=function(_1){if(_1==null){var _2=this}else{var _2=this.form.getItem(_1)}
if(!_2.hasDataElement())return null;if(!this.isDrawn())return;var _3=this.$14x;if(_3==null){_3=(this.$14x=isc.Element.get(this.getDataElementId()))}
return _3}
,isc.A.getOuterElement=function(){if(!this.isDrawn())return null;var _1=this.$158;if(this.$15s(_1)){return this.getOuterTableElement()}
if(this.showPickerIcon){return this.$15f()}
var _2=this.$15h();if(_2==null){_2=this.getHandle()}
return _2}
,isc.A.getHandle=function(){if(!this.isDrawn())return null;if(this.$11e())return this.getAbsDiv();if(this.containerWidget==this.form)return this.getFormCell();return isc.Element.get(this.$qs(this.$13j))}
,isc.A.getOuterTableElement=function(){return this.$15c(this.$133)}
,isc.A.$168=function(_1){if(!_1)_1=isc.EH.lastEvent;var _2=_1.itemInfo;return(_2&&_2.overElement)}
,isc.A.$169=function(_1){if(!_1)_1=isc.EH.lastEvent;var _2=_1.itemInfo;return(_2&&(_2.overTextBox||_2.overElement))}
,isc.A.$17a=function(_1){if(!_1)_1=isc.EH.lastEvent;var _2=_1.itemInfo;return(_2&&(_2.overControlTable||this.$169(_1)||(_2.overIcon&&this.$117(_2.overIcon)==this.getPickerIcon())))}
,isc.A.getFormCellID=function(){return this.$qs(this.$14k)}
,isc.A.getFormCell=function(){return isc.Element.get(this.getFormCellID())}
,isc.A.getDisplayValue=function(_1){var _2;return this.mapValueToDisplay(_1!==_2?_1:this.getValue())}
,isc.A.mapValueToDisplay=function(_1){var _2=this.$17b(_1);_1=this.$17c(_2);return _1}
,isc.A.$17c=function(_1){if(this.applyStaticTypeFormat){if(this.formatValue!=null){var _2=this.form,_3=this.form?this.form.values:{};return this.formatValue(_1,_3,_2,this)}}else if(this.formatEditorValue!=null){var _2=this.form,_3=this.form?this.form.values:{};return this.formatEditorValue(_1,_3,_2,this)}
if(this.dateFormatter!=null&&isc.isA.Date(_1))
return _1.toNormalDate(this.dateFormatter);if(this.timeFormatter!=null&&isc.isA.Date(_1))
return isc.Time.toTime(_1,this.timeFormatter);if(this.$64&&this.applyStaticTypeFormat){return this.$62.normalDisplayFormatter(_1,this,this.form,this.form.values)}
if(_1==null)_1=this.emptyDisplayValue;else{_1=isc.iscToLocaleString(_1);if(this.emptyDisplayValue!=isc.emptyString&&_1==isc.emptyString)
_1=this.emptyDisplayValue}
return _1}
,isc.A.mapDisplayToValue=function(_1){if(!this.applyStaticTypeFormat&&this.parseEditorValue!=null){return this.parseEditorValue(_1,this.form,this)}
return this.$17d(_1)}
,isc.A.$17b=function(_1,_2){var _3=_2?null:_1;var _4=this.getValueMap();if(!_4)return _3;if(isc.isA.String(_4))_4=this.getGlobalReference(_4);if(isc.isAn.Array(_4))return _3;var _5=isc.getValueForKey(_1,_4,_3);return _5}
,isc.A.$17d=function(_1){var _2=this.getValueMap();if(!_2)return _1;if(isc.isA.String(_2))_2=this.getGlobalReference(_2);if(isc.isAn.Array(_2))return _1;return isc.getKeyForValue(_1,_2)}
,isc.A.setValueMap=function(_1){this.valueMap=_1;this.updateValueMap()}
,isc.A.setValueIcons=function(_1){this.valueIcons=_1;if(this.isDrawn())this.redraw()}
,isc.A.setOptions=function(_1){return this.setValueMap(_1)}
,isc.A.updateValueMap=function(_1){if(_1!=false){this.setElementValue(this.mapValueToDisplay(this.getValue()))}
if(this.hasElement())this.setElementValueMap(this.getValueMap())}
,isc.A.setElementValueMap=function(_1){}
,isc.A.getValueMap=function(){var _1=this.valueMap;if(isc.isA.String(_1)){_1=this.getGlobalReference(_1)}
var _2=this.$43d;if(_2!=null){if(_1==null)_1=_2;else{if(isc.isAn.Array(_1)){var _3=_1;_1={};for(var i=0;i<_3.length;i++){_1[_3[i]]=_3[i]}}
isc.addProperties(_1,_2)}}
return _1}
,isc.A.getValueFieldName=function(){if(this.valueField)return this.valueField;if(this.form.dataSource&&this.foreignKey)
return isc.DS.getForeignFieldName(this,this.form.dataSource);var _1=this.getFieldName(),_2=this.getOptionDataSource();if(_2&&_2.getField(_1)==null){_1=_2.getTitleField()}
return _1||"name"}
,isc.A.getDisplayFieldName=function(){if(this.displayField)return this.displayField;var _1=this.getOptionDataSource();if(_1&&_1!=isc.DataSource.getDataSource(this.form.dataSource)&&_1.getField(this.getValueFieldName()).hidden==true){return _1.getTitleField()}}
,isc.A.$426=function(){if(this.displayField!=null){var _1=this.form.getValues(),_2=_1[this.getFieldName()],_3=_1[this.displayField];if(_3!=null){var _4={};_4[_2]=_3}
this.$43d=_4}}
,isc.A.getOptions=function(){return this.getValueMap()}
,isc.A.getOptionDataSource=function(){var _1=this.optionDataSource;if(_1==null&&this.form.dataSource){if(this.foreignKey)_1=isc.DS.getForeignDSName(this,this.form.dataSource);else _1=this.form.dataSource}
if(isc.isA.String(_1))_1=isc.DataSource.getDataSource(_1);return _1}
,isc.A.getValueMapTitle=function(_1){var _2=this.getValueMap();if(isc.isAn.Array(_2))return(_2.contains(_1)?_1:"");return _2[_1]}
,isc.A.saveValue=function(_1,_2){var _3;this._value=_1;this.$14u=_2;if(this.isDrawn()){if(this.$11d())this.$17e(_1)}
if(_1==_3&&this.$17f){this.form.clearItemValue(this)}else{this.form.saveItemValue(this,_1)}}
,isc.A.$17e=function(_1){var _2=this.$15b();if(_2)_2.value=_1}
,isc.A.setValue=function(_1,_2){this.$17g=true;var _3=(this.maintainSelectionOnTransform&&this.hasFocus&&(this.$17h()!=this.$14l));if(_3)this.rememberSelection();if(this.$17i!=null){isc.Timer.clearTimeout(this.$17i);this.$17i=null}
var _4;if(_1==null&&!_2){var _5=this.getDefaultValue();if(_5!=null){_4=true;_1=_5}}
if(this.length!=null&&_1!=null&&isc.isA.String(_1)&&_1.length>this.length)
{_1=_1.substring(0,this.length)}
this.saveValue(_1,_4);if(this.fetchMissingValues&&_1!=null&&this.getDisplayFieldName()!=null&&(!this.$43e||!this.filterLocally))
{this.$43f(_1)}
var _6=this.mapValueToDisplay(_1);this.setElementValue(_6,_1);if(_3)this.resetToLastSelection(true);return _1}
,isc.A.setDefaultValue=function(_1){var _2=this.defaultValue,_3;this.defaultValue=_1;if(this.isSetToDefaultValue()||(this._value==null&&_2===_3))
this.clearValue()}
,isc.A.$43f=function(_1){var _2=(this.$17b(_1,true)!=null);if(!_2){var _3=this.getOptionDataSource();if(_3&&(this.$63l==null||!this.$63l[_1]))
{if(!this.$63l)this.$63l={};this.$63l[_1]=true;var _4=this.optionCriteria||{};if(!this.filterLocally){_4[this.getValueFieldName()]=_1}
var _5=isc.addProperties({},this.optionFilterContext,{showPrompt:false,clientContext:{dataValue:_1}});var _6;if(this.optionOperationId!==_6){_5.operationId=this.optionOperationId}
_3.fetchData(_4,{target:this,methodName:"fetchMissingValueReply"},_5)}}}
,isc.A.fetchMissingValueReply=function(_1,_2,_3){var _4,_5=_1.clientContext.dataValue,_6=this.getDisplayFieldName(),_7=this.getValueFieldName();delete this.$63l[_5];if(_2)_4=_2.find(_7,_5);if(!_4){this.logInfo("Unable to retrieve display value for data value:"+_5+" from dataSource "+this.getOptionDataSource());if(!this.filterLocally)return}
if(this.$43d==null)this.$43d={};var _8=this.$43d;var _9;for(var i=0;i<_2.length;i++){_4=_2[i];var _11=_4[_7],_12=_4[_6];if(_8[_11]!==_9){if(_8[_11]!=_12){this.logWarn("Deriving valueMap for '"+_7+"' from dataSource based on displayField '"+_6+"'. This dataSource contains more than one record with "+_7+" set to "+_11+" with differing "+_6+" values."+" Derived valueMap is therefore unpredictable.")}
continue}
_8[_4[_7]]=_4[_6]}
if(this.filterLocally)this.$43e=true;var _13=(this._value==_5);this.updateValueMap(_13)}
,isc.A.clearValue=function(){this.$17f=true;this.setValue();delete this.$17f}
,isc.A.setElementValue=function(_1,_2){if(!this.isDrawn())return;var _3;if(_2===_3){_2=this._value}
if(this.hasDataElement()){var _4=this.getDataElement();if(_4!=null){this.$17j(_2);return _4.value=_1}}
var _5=this.$15h();if(_5!=null){if(this.showValueIconOnly)_1=isc.emptyString;var _6=this.$xq(_2);if(_6!=null)
_1=_6+(_1!=null?_1:isc.emptyString);_5.innerHTML=_1;if(!this.clipValue||this.height==null||this.width==null){this.adjustOverflow("textBox value changed")}}}
,isc.A.$17j=function(_1){if(this.suppressValueIcon||!this.isDrawn())return;var _2=this.$144(_1),_3=this.$15v();if(_2!=null){if(this.imageURLSuffix!=null)_2+=this.imageURLSuffix;_2=isc.Canvas.getImgURL(_2,this.imageURLPrefix||this.baseURL||this.imgDir);if(_3!=null){_3.src=_2}else{var _4=false;if(this.hasDataElement()){var _5=this.getDataElement();if(_5!=null){isc.Element.insertAdjacentHTML(_5,"beforeBegin",this.$xq(_1));_5.style.width=this.getTextBoxWidth(_1);_4=true}}else{var _6=this.$15h();if(_6!=null){isc.Element.insertAdjacentHTML(_6,"afterBegin",this.$xq(_1));_4=true}}
if(!_4)this.redraw()}}else if(_3!=null){isc.Element.clear(_3);if(this.hasDataElement()){var _5=this.getDataElement();_5.style.width=this.getTextBoxWidth(_1)}}}
,isc.A.setHint=function(_1){this.hint=_1;if(this.showHint)this.redraw()}
,isc.A.setHintStyle=function(_1){if(!this.$712()&&this.getHint()){var _2=this.$59t();if(_2)_2.className=_1}}
,isc.A.$66t=function(){if(!this.$66u){if(this.hasDataElement()){var _1=this.getDataElement();_1.className=this.$66v()}else{var _2=this.$15h();if(_2!=null)
_2.className=this.$66v()}
var _3=this.getHint();if(_3)_3=_3.unescapeHTML();this.setElementValue(_3);this.$66u=true}}
,isc.A.$66s=function(){if(this.$66u){if(this.hasDataElement()){var _1=this.getDataElement();_1.className=this.getTextBoxStyle()}else{var _2=this.$15h();if(_2!=null)
_2.className=this.getTextBoxStyle()}
this.setElementValue(isc.emptyString);this.$66u=false}}
,isc.A.$66v=function(){return this.textBoxStyle+"Hint"}
,isc.A.$712=function(){if(this.showHint&&this.showHintInField){if(isc.isA.TextItem(this)||isc.isA.TextAreaItem(this)||isc.isA.SelectItem(this))
{return true}}
return false}
,isc.A.getDefaultValue=function(){if(this.defaultDynamicValue){this.convertToMethod("defaultDynamicValue");var _1=this,_2=this.form,_3=this.form.getValues();return this.defaultDynamicValue(_1,_2,_3)}
return this.defaultValue}
,isc.A.setToDefaultValue=function(){return this.clearValue()}
,isc.A.isSetToDefaultValue=function(){return(this.$14u==true)}
,isc.A.updateValue=function(){if(!this.hasElement()||this.getDataElement()==null)return;var _1=this.getElementValue();return this.$10y(_1)}
,isc.A.$10y=function(_1){if(this.$17k){_1=this.$17l(_1)}
_1=this.mapDisplayToValue(_1);if(this.compareValues(_1,this._value))return true;if(this.$17m){if(this.compareValues(_1,this.$17n))return true}
var _2=this.handleChange(_1,this._value);if(this.destroyed)return;_1=this.$17n;this.updateAppearance(_1);if(!this.compareValues(_1,this._value))this.saveValue(_1);delete this.$17n;this.handleChanged(this._value);return _2}
,isc.A.handleChanged=function(_1){if(this.changed)this.changed(this.form,this,_1);if(this.form&&this.form.itemChanged!=null){this.form.itemChanged(this,_1)}}
,isc.A.updateAppearance=function(_1){if(this.valueIcons||this.getValueIcon){this.$17j(_1)}}
,isc.A.getValue=function(){var _1;if(this._value!==_1){return this._value}
return this.form.getSavedItemValue(this)}
,isc.A.getElementValue=function(){if(this.$66u)return null;var _1=this.getDataElement();if(!_1)return null;return _1.value}
,isc.A.resetValue=function(){var _1=this.form.$10s[this.getFieldName()];this.setValue(_1)}
,isc.A.shouldSubmitValue=function(){return this.form.$11q()}
,isc.A.isEditable=function(){return true}
,isc.A.getCriteriaFieldName=function(){return this.getFieldName()}
,isc.A.getCriteriaValue=function(){return this.getValue()}
,isc.A.getCriterion=function(){var _1=this.getValue();if(_1==null||isc.is.emptyString(_1))return;if(isc.isAn.Array(_1)){if(_1.length==0||_1.contains(isc.emptyString))return}
if(this.operator){var _2=this.operator}else{if(this.valueMap||this.optionDataSource||isc.SimpleType.inheritsFrom(this.type,"enum")){_2="equals"}else{_2="iContains"}}
return{fieldName:this.criteriaField?this.criteriaField:this.name,operator:_2,value:_1}}
,isc.A.clearErrors=function(){var _1=this.getFieldName();if(_1)this.form.clearFieldErrors(_1,true)}
,isc.A.setError=function(_1){var _2=this.getFieldName();if(_2)this.form.setError(_2,_1)}
,isc.A.hasErrors=function(){if(this.parentItem!=null)return this.parentItem.hasErrors();var _1=this.getFieldName();if(_1&&this.form)return this.form.hasFieldErrors(_1);return false}
,isc.A.validate=function(){var _1=this.hasErrors(),_2=[],_3=null,_4=false;var _5=this.getValue(),_6=isc.addProperties({},this.form.getValues()),_7={unknownErrorMessage:this.form.unknownErrorMessage};var _8=this.form.validateFieldAndDependencies(this,this.validators,_5,_6,_7);if(_8!=null){if(_8.resultingValue!=null){this.setValue(_8.resultingValue)}
if(!_8.valid){_2=_8.errors[this.name];if(_2==null)_2=[]}
_4=_8.stopOnError;_3=_8.errors}
var _9=false;if(_2.length>0||_1){if(_2.length>0){this.form.setFieldErrors(this.name,_2,false)}else{this.form.clearFieldErrors(this.name,false)}
_9=true;if(_4)this.focusInItem()}
for(var _10 in _3){if(_10!=this.name){var _11=_3[_10];if((_11!=null&&!isc.isAn.emptyObject(_11))||this.form.hasFieldErrors(_10))
{this.form.setFieldErrors(_10,_11,false);_9=true}}}
if(_9){this.redraw()}
return(_2.length==0)}
,isc.A.setAutoComplete=function(_1){this.autoComplete=_1;this.$10q()}
,isc.A.$10q=function(){var _1=this.$17h();if(isc.Browser.isIE&&this.hasDataElement()){var _2=this.getDataElement();if(_2)_2.autoComplete=(_1=="native"?"":"off")}}
,isc.A.$17h=function(){if(this.autoComplete!=null)return this.autoComplete;return this.form.autoComplete}
,isc.A.autoCompleteEnabled=function(){if(isc.Browser.isSafari)return false;return this.$17h()=="smart"}
,isc.A.uniqueMatchOnly=function(){if(this.uniqueMatch!=null)return this.uniqueMatch;return this.form.uniqueMatch}
,isc.A.getCandidates=function(){var _1=this.autoCompleteCandidates;if(_1==null){var _2=this.getValueMap();if(_2!=null){if(isc.isAn.Array(_2))_1=_2;else _1=isc.getValues(_2)}else if(this.form.grid){var _3=this.form.grid.data;if(isc.isA.ResultSet!=null&&isc.isA.ResultSet(_3))_1=_3.getValuesList(this.name);else _1=_3.getProperty(this.name)}}
if(_1!=null)_1=_1.getUniqueItems();return _1}
,isc.A.getCompletion=function(_1){if(_1==null)return;var _2=this.getCandidates();if(_2==null||_2.length==0)return;var _3=_1.toUpperCase(),_4=this.uniqueMatchOnly(),_5;for(var i=0;i<_2.length;i++){var _7=_2[i],_8=_7!=null?_7.toUpperCase():null;if(_8==_3)return null;if(isc.startsWith(_8,_3)){if(!_4)return _7;if(_5!=null)return null;_5=_7}}
return _5}
,isc.A.showCompletion=function(_1){this.clearCompletion();if(!this.canAutoComplete||!this.hasDataElement()||!this.autoCompleteEnabled())return;var _2=isc.EH.lastEvent.keyName;if(_2=="Backspace"||_2=="Delete")return;var _3=this.getCompletion(_1);if(_3==null){return}
this.form.$106();this.form.$11b(this);this.setElementValue(_1+_3.substring(_1.length));this.$17o=_1;this.$17k=_3;this.setSelectionRange(_1.length,_3.length)}
,isc.A.$17l=function(_1){var _2=this.$17k,_3=isc.EH.lastEvent.keyName;if(this.$14m[_3]){this.acceptCompletion(_3==this.$10j);return _2}
var _4=_2.substring(this.$17o.length);if(!_1.endsWith(_4)){this.clearCompletion();return _1}
if(this.getSelectedText()==_4){return this.$17o}
this.clearCompletion();return _1}
,isc.A.clearCompletion=function(){delete this.$17k;delete this.$17o}
,isc.A.acceptCompletion=function(_1){var _2=this.$17k;if(!_2)return;if(this.autoCompleteEnabled()){var _3=_1?[_2.length,_2.length]:null;if(this.getElementValue()!=_2){if(!_1)_3=this.getSelectionRange();this.setElementValue(_2)}
if(this.hasFocus&&_3)
this.setSelectionRange(_3[0],_3[1])}
this.clearCompletion()}
,isc.A.setSelectionRange=function(_1,_2){if(!isc.isA.TextItem(this)&&!isc.isA.TextAreaItem(this))return;if(!this.isDrawn())return;if(!isc.isA.Number(_1))_1=0;if(!isc.isA.Number(_2))_2=0;if(_1>_2){var _3=_2;_2=_1;_1=_3}
var _4=this.getDataElement();if(_4==null)return;if(isc.Browser.isIE){var _5=_4.createTextRange();_5.collapse(true);_5.moveStart(this.$14n,_1);_5.moveEnd(this.$14n,(_2-_1));_5.select()}else{_4.focus();_4.setSelectionRange(_1,_2)}}
,isc.A.selectValue=function(){var _1=this.getElementValue(),_2=isc.isA.String(_1)?_1.length:0;this.setSelectionRange(0,_2)}
,isc.A.deselectValue=function(_1){if(!this.hasFocus)return;if(_1)this.setSelectionRange(0,0);else{var _2=this.getElementValue(),_3=isc.isA.String(_2)?_2.length:0;this.setSelectionRange(_3,_3)}}
,isc.A.getSelectionRange=function(_1){if(!isc.isA.TextItem(this)&&!isc.isA.TextAreaItem(this))return;if(isc.isA.UploadItem(this))return;if(!this.hasFocus)return;var _2=this.getDataElement();if(_2==null)return;if(isc.Browser.isIE){var _3=[],_4=this.$17p();var _5=_2.createTextRange();if(_5==null||_4==null)return;if(isc.isA.TextAreaItem(this)){if(!this.supportsSelectionRange)return null;var _6=_5.text.length;if(_6==_4.text.length){return[0,_6]}else{if(_1){if(_5.offsetLeft==_4.offsetLeft&&_5.offsetTop==_4.offsetTop)
{return[0,_4.text.length]}else{_5.collapse(false);if(_5.offsetLeft==_4.offsetLeft&&_5.offsetTop==_4.offsetTop)
{return[_6,_6]}}}else{for(var i=0;i<=_6;i++){if(_5.offsetLeft==_4.offsetLeft&&_5.offsetTop==_4.offsetTop)
{return[i,i+_4.text.length]}
_5.moveStart(this.$14n)}}}
return null}
if(_5.compareEndPoints(this.$14o,_4)==0){_3[1]=_5.text.length}else{_5.setEndPoint(this.$14o,_4);_3[1]=_5.text.length}
_5.setEndPoint(this.$14p,_4);_3[0]=_5.text.length;return _3}else if(isc.Browser.isMoz||isc.Browser.isSafari){return[_2.selectionStart,_2.selectionEnd]}}
);isc.evalBoundary;isc.B.push(isc.A.getSelectedText=function(){if(!isc.isA.TextItem(this)&&!isc.isA.TextAreaItem(this)){return}
if(isc.Browser.isIE){var _1=this.$17p();if(_1)return _1.text}else if(isc.Browser.isMoz||isc.Browser.isSafari){var _2=this.getElement();if(_2!=null){return _2.value.substring(_2.selectionStart,_2.selectionEnd)}}}
,isc.A.$17p=function(){if(!isc.Browser.isIE)return;if(isc.isA.TextAreaItem(this)&&!this.supportsSelectionRange)return null;var _1=this.getDocument().selection,_2=(_1!=null?_1.createRange():null);if(_2!=null&&_2.parentElement().id==this.getDataElementId())return _2;return null}
,isc.A.rememberSelection=function(){if(!this.isDrawn())return;if(!isc.isA.TextItem(this)&&!isc.isA.TextAreaItem(this))return;var _1=this.getElementValue();if(_1==isc.emptyString)return;this.$17q=_1;var _2=this.getSelectionRange();if(_2){this.$17r=_2[0];this.$17s=_2[1]}}
,isc.A.resetToLastSelection=function(_1){if(!this.isDrawn()||this.$17r==null)return;var _2,_3=this.getElementValue(),_4=this.$17q;if(!_1)_2=(_3==_4);else{if(this.$17r==0&&this.$17s==_4.length){_2=true;this.$17s=_3.length}else{_2=(_3.toLowerCase()==_4.toLowerCase())}}
if(_2)this.setSelectionRange(this.$17r,this.$17s);delete this.$17r;delete this.$17s;delete this.$17q}
,isc.A.handleChange=function(_1,_2){if(this.$17m&&this.compareValues(_1,this.$17n))return true;this.$17m=true;this.$17n=_1;var _3=_1;if(isc.isA.Date(_3))_3=_3.duplicate();else if(isc.isAn.Array(_3))_3=_3.duplicate();else if(isc.isAn.Object(_3))_3=isc.addProperties({},_3);if(this.transformInput){_1=this.transformInput(this.form,this,_1,_2)}
var _4=this.hasErrors(),_5=false,_6=[],_7=null,_8;if(this.length!=null&&isc.isA.String(_1)&&_1.length>this.length){_1=_1.substring(0,this.length)}
var _9=isc.addProperties({},this.form.getValues()),_10={unknownErrorMessage:this.form.unknownErrorMessage,changing:true},_11=this.form.validateFieldAndDependencies(this,this.validators,_1,_9,_10);if(_11!=null){_5=!_11.valid;if(_11.resultingValue!=null){_8=_11.resultingValue}
if(!_11.valid){_6=_11.errors[this.name];if(_6==null)_6=[]}
_7=_11.errors}
var _12;if(_5&&_8===_12&&this.$65v())
{_8=_2;if(_2==null)_8=null}
if(_8!==_12)_1=_8;if(_5){this.clearErrors();this.setError(_6)}else if(_11!=null&&_4){this.clearErrors()}
this.$17g=false;if((!_5||this.changeOnError)){if(this.change!=null){if(this.change(this.form,this,_1,_2)==false){_1=_2;_5=true}}
if(this.destroyed)return;if(!_5&&this.form&&this.form.itemChange!=null){if(this.form.itemChange(this,_1,_2)==false){_1=_2;_5=true}}}
var _13=this.$17g;var _14=!this.compareValues(_1,_3);if((_5||_14)&&!_13)
{this.setValue(_1);if(_5&&this.maintainSelectionOnTransform)this.$17t()}
if(this.$17g)this.$17n=this._value;if((this.redrawOnChange||_6.length>0||(_6.length==0&&_11!=null&&_4)))
{this.redraw()}
for(var _15 in _7){if(_15!=this.name){this.form.setFieldErrors(_15,_7[_15],true)}}
if(!_5&&this.hasFocus)this.showCompletion(_1);delete this.$17m;return(!_5)}
,isc.A.$65v=function(){return(this.rejectInvalidValueOnChange!=null)?this.rejectInvalidValueOnChange:this.form.rejectInvalidValueOnChange}
,isc.A.compareValues=function(_1,_2){return isc.DynamicForm.compareValues(_1,_2)}
,isc.A.elementChanged=function(){isc.EH.$h1("ICHG");this.logDebug("native change");this.updateValue();isc.EH.$h2();return true}
,isc.A.$680=function(_1,_2,_3){if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("handling inactive editor event:"+_1+", inactive context:"+this.echo(_2),"inactiveEditorHTML")}
var _4=this.$683[_1];if(_4==null){_4=this.$683[_1]="inactiveEditor"+_1.substring(0,1).toUpperCase()+_1.substring(1)}
if(this[_4]!=null){return this[_4](_2,_3)}}
,isc.A.$17u=function(_1){this.convertToMethod(_1);return this[_1](this.form,this,isc.EH.lastEvent)}
,isc.A.handleTitleClick=function(){if(this.isDisabled())return;if(this.editingOn){this.editClick();this.handleClick();return false}
return this.$17u("titleClick")}
,isc.A.handleTitleDoubleClick=function(){if(this.isDisabled())return;return this.$17u("titleDoubleClick")}
,isc.A.handleClick=function(){if(this.editingOn){isc.EditContext.selectCanvasOrFormItem(this,true);return false}
if(this.isDisabled())return;return this.$17u("click")}
,isc.A.handleDoubleClick=function(){if(this.isDisabled())return;return this.$17u("doubleClick")}
,isc.A.handleCellClick=function(){if(this.isDisabled())return;return this.$17u("cellClick")}
,isc.A.handleCellDoubleClick=function(){if(this.isDisabled())return;return this.$17u("cellDoubleClick")}
,isc.A.$12z=function(){return this.form.elementChanged(this.getID())}
,isc.A.handleMouseMove=function(){if(!this.isDisabled()&&(this.showValueIconOver||this.showValueIconDown)){var _1=isc.EH.lastEvent.itemInfo,_2=(_1.overElement||_1.overTextBox||_1.overControlTable),_3=this.$15t;if(_2){if(this.$j6&&this.showValueIconDown){if(_3!=this.$13v){this.$15t=this.$13v;this.$17j()}}else if(this.showValueIconOver&&_3!=this.$13u){this.$15t=this.$13u;this.$17j()}}else{var _4=(this.showValueIconFocused&&this.showValueIconOver&&this.hasFocus)?this.$13u:null;if(_3!=_4){this.$15t=_4;this.$17j()}}}
if(this.$17u("mouseMove")==false)return false}
,isc.A.handleMouseOver=function(){isc.Hover.setAction(this,this.$wb,null,this.$17v());return this.$17u("mouseOver")}
,isc.A.handleMouseOut=function(){var _1=(this.showValueIconFocused&&this.showValueIconOver&&this.hasFocus)?this.$13u:null;if(this.$15t!=_1){this.$15t=_1;this.$17j()}
this.stopHover();return this.$17u("mouseOut")}
,isc.A.handleMouseDown=function(){var _1=isc.EH.lastEvent.itemInfo,_2=_1.inactiveContext;if(_2!=null){return this.form.bubbleInactiveEditorEvent(this,"mouseDown",_1)}
if(!this.isDisabled()&&this.showValueIconDown){var _3=(_1.overElement||_1.overTextBox||_1.overControlTable);if(_3){this.$15t=this.$13v;this.$j6=true;isc.Page.setEvent(isc.EH.MOUSE_UP,this,isc.Page.FIRE_ONCE,"$17w");this.$17j()}}
if(this.mouseDown)return this.$17u("mouseDown")}
,isc.A.stopHover=function(){isc.Hover.clear()}
,isc.A.$17w=function(){this.$j6=null;if(this.$15t==this.$13v){this.$15t=this.showValueIconOver?this.$13u:null;this.$17j()}}
,isc.A.handleMouseStillDown=function(_1){if(this.mouseStillDown){return this.$17u("mouseStillDown")}}
,isc.A.$17v=function(){return this.hoverDelay!=null?this.hoverDelay:this.form.itemHoverDelay}
,isc.A.handleTitleMove=function(){return this.$17u("titleMove")}
,isc.A.handleTitleOver=function(){isc.Hover.setAction(this,this.$17x,null,this.$17v());return this.$17u("titleOver")}
,isc.A.handleTitleOut=function(){this.stopHover();return this.$17u("titleOut")}
,isc.A.$121=function(_1,_2){var _3=this.$117(_1);if(_3!=null){var _4=(_3.prompt!=null?_3.prompt:this.iconPrompt)
window.status=_4;if(this.$54o(_3)){this.$164(_3,false,true)}
else if(this.$165(_3))this.$164(_3,true)}
return this.$12v(_2,this)}
,isc.A.$123=function(_1,_2){var _3=this.$117(_1);if(_3!=null){window.status="";var _4=this.$54o(_3),_5=_3.showFocusedWithItem!=false,_6=this.showIconsOnFocus;if(_4&&(!_6||!_5)){this.$164(_3,false,false)}
if(this.$165(_3))this.$164(_3,false,false)}
return this.$12x(_2,this)}
,isc.A.$113=function(_1){if(_1==this.getErrorIconId())return this.$17y();var _2=this.$117(_1);if(_2!=null){if(this.$165(_2))this.$164(_2,true);this.$17z=_2;isc.Hover.setAction(this,this.$170,null,this.$17v());var _3=(_2.prompt!=null?_2.prompt:this.iconPrompt)
window.status=_3;return true}}
,isc.A.$114=function(_1){if(_1==this.getErrorIconId())return this.$171();var _2=this.$117(_1);if(_2!=null){window.status="";if(this.$165(_2))this.$164(_2,false);delete this.$17z;isc.Hover.setAction(this,this.$wb,null,this.$17v());return true}}
,isc.A.$116=function(_1){var _2=this.$117(_1);if(_2==null)return;if(this.iconIsDisabled(_2))return;if(_2.click!=null){if(!isc.isA.Function(_2.click)){isc.Func.replaceWithMethod(_2,"click","form,item,icon")}
if(_2.click(this.form,this,_2)==false)return false}
if(this.iconClick)this.iconClick(this.form,this,_2)}
,isc.A.$172=function(_1){var _2=this.$117(_1);if(_2){var _3=isc.EH.getKey(),_4=isc.EH.getKeyEventCharacter();if(_2.keyPress){if(!isc.isA.Function(_2.keyPress)){isc.Func.replaceWithMethod(_2,"keyPress","keyName, character,form,item,icon")}
if(_2.keyPress(_3,_4,this.form,this,_2)==false)
return false}
if(this.iconKeyPress)this.iconKeyPress(_3,_4,this.form,this,_2);if(_3==this.$10j){if(this.$116(_2)==false)return false}}}
,isc.A.$17y=function(){}
,isc.A.$171=function(){}
,isc.A.$wb=function(_1){if(this.itemHover&&this.itemHover(this,this.form)==false)return false;var _2;if(this.itemHoverHTML)_2=this.itemHoverHTML(this,this.form);else _2=this.form.itemHoverHTML(this,this.form);this.form.$119(this,_2)}
,isc.A.$17x=function(_1){if(this.titleHover&&this.titleHover(this,this.form)==false)return false;var _2;if(this.titleHoverHTML)_2=this.titleHoverHTML(this,this.form);else _2=this.form.titleHoverHTML(this,this.form);this.form.$119(this,_2)}
,isc.A.$170=function(){if(this.itemHover&&this.itemHover(this,this.form)==false)return false;var _1=this.$17z,_2=this.getIconPrompt(_1);if(_2&&!isc.is.emptyString(_2))
isc.Hover.show(_2,this.form.$wc(this));else isc.Hover.setAction(this,this.$wb,null,this.$17v())}
,isc.A.getGlobalTabIndex=function(){if(this.globalTabIndex==null){if(this.tabIndex==-1)this.globalTabIndex=-1;else{var _1=this.form.getTabIndex(),_2=this.getTabIndex();if(_1==-1)return-1;return(_1+_2)}}
return this.globalTabIndex}
,isc.A.getTabIndex=function(){if(this.tabIndex!=null)return this.tabIndex;if(this.globalTabIndex||!this.$kk())return null;if(this.$10p==null){this.form.$10o()}
return this.$10p}
,isc.A.setGlobalTabIndex=function(_1){this.globalTabIndex=_1;this.$173(_1)}
,isc.A.setTabIndex=function(_1){this.globalTabIndex=null;this.tabIndex=_1;this.$173(_1)}
,isc.A.$154=function(_1){if(this.isInactiveHTML()||(!_1&&this.isDisabled()))
{return-1}
if(this.$174!=null)return this.$174;return this.getGlobalTabIndex()}
,isc.A.$173=function(_1){this.$174=_1;if(!this.$kk()||!this.isVisible()||!this.isDrawn())return;if(this.getFocusElement()!=null){isc.FormItem.setElementTabIndex(this.getFocusElement(),_1);this.$175()}else{this.redraw("set tab index")}}
,isc.A.updateTabIndex=function(){if(!this.$kk()||!this.isVisible()||!this.isDrawn()||this.isDisabled())return;var _1=this.getGlobalTabIndex();if(this.$174!=_1)this.$173(_1)}
,isc.A.$16p=function(_1){if(_1.tabIndex==-1||this.iconIsDisabled(_1))return-1;return this.$154(true)}
,isc.A.$175=function(){var _1=[];_1.addList(this.icons);if(this.showPickerIcon)_1.add(this.getPickerIcon());for(var i=0;i<_1.length;i++){var _3=_1[i];if(!_3||_3.imgOnly)continue;var _4=this.$16g(_3);if(_4!=null){isc.FormItem.setElementTabIndex(_4,this.$16p(_3))}}}
,isc.A.setDisabled=function(_1){var _2=this.isDisabled();this.disabled=_1;var _3=this.isDisabled();if(_2!=_3)this.updateDisabled()}
,isc.A.setShowDisabled=function(_1){this.showDisabled=_1;this.updateDisabled()}
,isc.A.updateDisabled=function(){var _1=this.isDisabled();this.$176(!_1);this.$177();this.$17j();if(this.showDisabled)this.updateState()}
,isc.A.setEnabled=function(_1){return this.setDisabled(!_1)}
,isc.A.isDisabled=function(){var _1=this.disabled
if(!_1){if(this.parentItem!=null)_1=this.parentItem.isDisabled();else{_1=this.form.isDisabled();if(!_1&&this.containerWidget!=this.form)_1=this.containerWidget.isDisabled()}}
return _1}
,isc.A.enable=function(){this.setDisabled(false)}
,isc.A.disable=function(){this.setDisabled(true)}
,isc.A.$176=function(_1){if(this.hasDataElement()){var _2=this.getDataElement();if(_2){_2.disabled=!_1;_2.tabIndex=this.$154();if(this.useDisabledEventMask()){var _3=this.$43b();if(_3&&(!_3.getAttribute||_3.getAttribute("isDisabledEventMask")!="true"))
{_3=null}
if(_1&&_3){isc.Element.clear(_3)}else if(!_1&&!_3){isc.Element.insertAdjacentHTML(_2,"beforeBegin",this.$148())}}}}else if(this.$kk()){var _2=this.getFocusElement();if(_2)_2.tabIndex=this.$154()}}
,isc.A.$177=function(){if(this.showPickerIcon){var _1=this.getPickerIcon();this.setIconEnabled(_1)}
if(!this.icons||this.icons.length<1)return;for(var i=0;i<this.icons.length;i++){this.setIconEnabled(this.icons[i])}}
,isc.A.iconIsDisabled=function(_1){_1=this.$117(_1);if(!_1)return;if(this.containerWidget&&this.containerWidget.isDisabled())return true;if(_1.neverDisable)return false;return this.isDisabled()}
,isc.A.$kk=function(){if(this.canFocus!=null)return this.canFocus;return this.hasDataElement()}
,isc.A.focusInItem=function(){var _1=this.isVisible()&&this.$kk()&&!this.isDisabled(),_2=_1?this.getFocusElement():null;if(!_1||!_2){if(this.parentItem)this.parentItem.focusInItem();return}
if(_2.focus){var _3=this.getActiveElement();if(!isc.Browser.isIE||_3!=_2){this.logInfo("about to call element.focus() "+isc.EH.$lb()+(this.logIsDebugEnabled("traceFocus")?this.getStackTrace():""),"nativeFocus");isc.FormItem.$12n(this);isc.EventHandler.$lg=this;_2.focus()}else{this.logInfo("element already focused, not focus()ing","nativeFocus")}
if(isc.Browser.isIE){isc.EH.$vm=this;this.$167=_2}}else{this.logInfo("can't call element focus, no element","nativeFocus")}
var _4=this.selectOnFocus;if(_4==null&&this.form)_4=this.form.selectOnFocus;if(_4&&_2.select)_2.select()}
,isc.A.blurItem=function(){if(!this.isVisible()||!(this.hasFocus||isc.EH.$vm==this))return;var _1=this.$166();if(_1&&_1.blur){this.logInfo("about to call element blur"+isc.EH.$lb()+(this.logIsDebugEnabled("traceBlur")?this.getStackTrace():""),"nativeFocus");isc.EH.$ld=this;_1.blur()}else{this.logInfo("can't call element blur, no element","nativeFocus")}}
,isc.A.focusInIcon=function(_1){_1=this.$117(_1);if(_1==null||_1.imgOnly)return;var _2=this.$16g(_1);if(_2!=null)_2.focus()}
,isc.A.blurIcon=function(_1){if(isc.isA.String(_1))_1=this.$117(_1);if(_1==null||!this.icons||!this.icons.contains(_1)||_1.imgOnly)return;var _2=this.$16g(_1);if(_2!=null)_2.blur()}
,isc.A.$12v=function(_1,_2){isc.EH.$h1("IFCS");if(isc.EH.$lg==this)delete isc.EH.$lg
isc.EH.$la(this,true);if(isc.Browser.isMoz&&!this.isVisible()){this.logWarn("calling element.blur() to correct focus in hidden item: "+this,"nativeFocus");_1.blur();return}
this.hasFocus=true;this.$167=_1;var _3=this.form.elementFocus(_1,_2);isc.EH.$h2();return _3}
,isc.A.$12x=function(_1,_2){isc.EH.$h1("IBLR");if(isc.EH.$ld==this)delete isc.EH.$ld
if(this.$17i!=null){isc.Timer.clearTimeout(this.$17i);this.$14r()}
isc.EH.$la(this);this.hasFocus=false;delete this.$167;var _3=this.form.elementBlur(_1,_2);isc.EH.$h2();return _3}
,isc.A.elementFocus=function(_1){if(this.prompt)this.form.showPrompt(this.prompt);if(this.showIconsOnFocus&&this.showIcons){this.showAllIcons(true)}else{if(this.icons)this.updateIconsForFocus(this.icons,true)}
if(this.showFocusedPickerIcon&&this.showPickerIcon){var _2=this.getPickerIcon();if(_2)this.updateIconsForFocus(_2,true)}
if(this.showFocused)this.updateState();if(this.showValueIconFocused&&this.showValueIconOver&&this.$15t==null){this.$15t=this.$13u;this.$17j()}
if(_1)return;if(this.form.blockOnFieldBusy(this))return false;this.handleEditorEnter()
if(this.focus){this.convertToMethod("focus");return this.focus(this.form,this)}
return true}
,isc.A.updateIconsForFocus=function(_1,_2){if(_1==null)return;_2=!!_2;if(!isc.isAn.Array(_1))_1=[_1];for(var i=0;i<_1.length;i++){if(this.$54o(_1[i],true)){var _4=this.$16v(_1[i]);if(_4!=null){isc.Canvas.$wg(_4,this.getIconURL(_1[i],false,null,_2))}}}}
,isc.A.elementBlur=function(){if(this.prompt)this.form.clearPrompt();if(this.showIconsOnFocus&&this.showIcons){if(this.$161==null){this.$161=this.delayCall("hideAllIcons",[],0)}}else if(this.hideIconsOnKeypress&&this.showIcons){this.showAllIcons()}else{if(this.icons)this.updateIconsForFocus(this.icons,false)}
if(this.showFocusedPickerIcon&&this.showPickerIcon){var _1=this.getPickerIcon();if(_1)this.updateIconsForFocus(_1,false)}
if(this.showFocused){this.updateState()}
if(this.$15t==this.$13u){this.$15t=null
this.$17j()}
this.acceptCompletion();this.handleEditorExit();if(this.blur){this.convertToMethod("blur");return this.blur(this.form,this)}
return true}
,isc.A.$11s=function(_1){var _2=this.items,_3=this.icons;if((_2==null||_2.length==0)&&(_3==null||_3.length==0)){return false}
var _4=this.getFocusIconIndex(),_5;if(_4==null){var _6=isc.EventHandler.lastEvent.keyTarget;if(_6==this)_5=0;else if(_2){_5=_2.indexOf(_6)}}
if((_5==null||_5==-1)&&_4==null){return false}
if(_1){if(_5!=null&&_2!=null){while(_5<_2.length-1){_5+=1
var _7=_2[_5];if(_7.$kk()){_7.focusInItem();return true}}}
if(_4==null)_4=-1;if(this.canTabToIcons!=false&&this.showIcons&&_3!=null){while(_4<_3.length-1){_4+=1;var _8=_3[_4];if(this.$16f(_8)&&!_8.imgOnly&&_8.tabIndex!=-1){this.focusInIcon(_8);return true}}}
return false}else{if(this.canTabToIcons!=false&&_4!=null){while(_4>0){_4-=1;var _8=_3[_4]
if(this.$16f(_8)&&!this.imgOnly&&_8.tabIndex!=-1){this.focusInIcon(_8);return true}}}
if(_5==null)
_5=_2!=null?_2.length:1;while(_5>0){var _7;_5-=1
if(_2==null){_7=this}else{_7=_2[_5]}
if(_7.$kk()){_7.focusInItem();return true}}
return false}}
,isc.A.getFocusIconIndex=function(){var _1=this.$166();if(_1==null||this.icons==null)return null;for(var i=0;i<this.icons.length;i++){if(this.$16g(this.icons[i])==_1)return i}
return null}
,isc.A.$kr=function(_1,_2){if(_2.overTitle)return;if(_2.overIcon)return false;if(_1==null)_1=isc.EH.lastEvent;if(_1.nativeTarget&&(_1.nativeTarget.tagName.toLowerCase()==this.$54n))
return false;return this.canSelectText!=false}
,isc.A.handleEditorExit=function(){if(!this.$178)return;this.$178=null;var _1=this.getValue();if(this.validateOnExit||this.form.validateOnExit){if(!this.compareValues(_1,this.$75h))this.validate();this.$75h=null}
if(this.editorExit)this.editorExit(this.form,this,_1)}
,isc.A.handleEditorEnter=function(){if(this.$178)return;this.$178=true;var _1=this.getValue();if(this.validateOnExit||this.form.validateOnExit)this.$75h=_1;if(this.editorEnter)this.editorEnter(this.form,this,_1)}
,isc.A.$179=function(){var _1=this;this.$18a=isc.Page.setEvent(isc.EH.MOUSE_UP,function(){if(!_1.destroyed)_1.$18b()})}
,isc.A.$18b=function(){isc.Page.clearEvent(isc.EH.MOUSE_UP,this.$18a);delete this.$18a;if(this.getActiveElement()==document.body){this.focusInItem()}}
,isc.A.$429=function(){return false}
,isc.A.$43g=function(){if(this.changeOnKeypress&&!this.mask)this.updateValue()}
,isc.A.$43a=function(_1,_2){if(this.changeOnKeypress)this.$18c()}
,isc.A.handleKeyPress=function(_1,_2){if(!this.form)return;var _3=this.form.$ne(_1);if(_3.overIcon){if(this.$172(_3.overIcon)==false)return false}else{if(!this.$429()&&this.changeOnKeypress)this.$18c();var _4=_1.keyName;if(this.hideIconsOnKeypress&&!this.$16y&&_4!=this.$jf){this.hideAllIcons()}}
return this.$18d(this,this.form,_4,_1.characterValue)}
,isc.A.$18d=function(_1,_2,_3,_4){if(this.keyPress!=null&&this.keyPress(_1,_2,_3,_4)==false){return false}
if(!this.form)return false;if(this.parentItem==null&&this.form.itemKeyPress!=null){return this.form.itemKeyPress(_1,_3,_4)}}
,isc.A.$18c=function(){if(this.$17i!=null){isc.Timer.clearTimeout(this.$17i);this.$14r()}
if(this.maintainSelectionOnTransform&&(this.$17h()!=this.$14l))
{this.$18e()}
this.$17i=isc.Timer.setTimeout({target:this,methodName:this.$14q},0)}
,isc.A.$14r=function(){delete this.$17i;this.updateValue();this.$18f()}
,isc.A.$18e=function(){if(this.$18g!=null)return;var _1=this.getSelectionRange(true);if(_1){this.$18g=_1[0];this.$18h=_1[1]}}
,isc.A.$17t=function(){if(this.$18g==null)return;this.setSelectionRange(this.$18g,this.$18h)}
,isc.A.$18f=function(){delete this.$18g;delete this.$18h}
,isc.A.handleKeyDown=function(_1,_2){if(this.dirtyOnKeyDown)this.$18i();var _3=this,_4=this.form,_5=_1.keyName;if(this.keyDown!=null&&this.keyDown(_3,_4,_5)==false)return false}
,isc.A.$10v=function(){return this.$18j==true}
,isc.A.$18i=function(){this.$18j=true}
,isc.A.$10x=function(){this.$18j=false}
,isc.A.handleKeyUp=function(_1,_2){if(!this.form)return;var _3=this,_4=this.form,_5=_1.keyName;if(this.keyUp!=null&&this.keyUp(_3,_4,_5)==false)return false}
,isc.A.getSerializeableFields=function(_1,_2){_1.addList(["form"]);return this.Super("getSerializeableFields",arguments)}
,isc.A.getLeft=function(){var _1=this.isDrawn()?this.getOuterElement():null;if(_1==null){var _2="getLeft() Unable to determine position for "+(this.name==null?"this item ":this.name)+". ";if(this.isDrawn()){_2+="This method is not supported by items of type "+this.getClass()}else{_2+="Position cannot be determined before the element is drawn"}
_2+=" - returning zero.";this.form.logWarn(_2);return 0}
return this.$18k(_1)}
,isc.A.getTitleLeft=function(){var _1=this.isDrawn()&&this.form?isc.Element.get(this.form.$427(this)):null;if(_1==null){var _2="getTitleLeft() Unable to determine position for "+(this.name==null?"this item ":this.name)+". ";if(this.isDrawn()){_2+="This method is not supported by items of type "+this.getClass()}else{_2+="Position cannot be determined before the element is drawn"}
_2+=" - returning zero.";this.form.logWarn(_2);return 0}
return this.$18k(_1)}
,isc.A.$18k=function(_1){var _2=_1.offsetParent,_3=this.containerWidget.getHandle(),_4=_3.offsetParent,_5=isc.Element.getOffsetLeft(_1);while(_2!=_3&&_2!=_4){_5+=isc.Element.getOffsetLeft(_2)
_5-=(_2.scrollLeft||0);var _6=(isc.Browser.isIE?parseInt(_2.currentStyle.borderLeftWidth):parseInt(isc.Element.getComputedStyleAttribute(_2,"borderLeftWidth")));if(isc.isA.Number(_6))_5+=_6;var _7=(isc.Browser.isIE?parseInt(_2.currentStyle.marginLeft):parseInt(isc.Element.getComputedStyleAttribute(_2,"marginLeft")));if(isc.isA.Number(_7))_5+=_7;_2=_2.offsetParent}
if(_2==_4){_5-=isc.Element.getOffsetLeft(_3)}
return _5}
,isc.A.$145=function(_1){return(_1!=null&&(this.icons&&this.icons.contains(_1)||this.showPickerIcon&&this.getPickerIcon()==_1))}
,isc.A.getIconLeft=function(_1){if(_1==null&&this.icons!=null&&this.icons.getLength()>0)_1=this.icons[0];else if(!this.$145(_1)){this.logWarn("getIconLeft() passed invalid icon:"+isc.Log.echoAll(_1));return null}
var _2=this.$16v(_1);if(_2==null){this.logWarn("getIconLeft() unable to return position of icon - "+"this icon is not currently drawn into the page. Returning null");return null}
return isc.Element.$s4(_2,this.containerWidget.getClipHandle())}
,isc.A.getTop=function(){var _1=this.isDrawn()?this.getOuterElement():null;if(_1==null){var _2="getTop() Unable to determine position for "+(this.name==null?"this item ":this.name)+". ";if(this.isDrawn()){_2+="This method is not supported by items of type "+this.getClass()}else{_2+="Position cannot be determined before the element is drawn"}
_2+=" - returning zero.";this.form.logWarn(_2);return 0}
var _3=this.$18l(_1);return _3}
,isc.A.getTitleTop=function(){var _1=this.isDrawn()&&this.form?isc.Element.get(this.form.$427(this)):null;if(_1==null){var _2="getTitleTop() Unable to determine position for "+(this.name==null?"this item ":this.name)+". ";if(this.isDrawn()){_2+="This method is not supported by items of type "+this.getClass()}else{_2+="Position cannot be determined before the element is drawn"}
_2+=" - returning zero.";this.form.logWarn(_2);return 0}
return this.$18l(_1)}
,isc.A.$18l=function(_1){var _2=this.containerWidget.getHandle(),_3=_2.offsetParent,_4=_1.offsetParent,_5=isc.Element.getOffsetTop(_1);while(_4!=_2&&_4!=_3){_5+=isc.Element.getOffsetTop(_4)
_5-=(_4.scrollTop||0);var _6=(isc.Browser.isMoz?0:(isc.Browser.isIE?parseInt(_4.currentStyle.borderTopWidth):parseInt(isc.Element.getComputedStyleAttribute(_4,"borderTopWidth"))));if(isc.isA.Number(_6))_5+=_6;var _7=(isc.Browser.isIE?parseInt(_4.currentStyle.marginTop):parseInt(isc.Element.getComputedStyleAttribute(_4,"marginTop")));if(isc.isA.Number(_7))_5+=_7;_4=_4.offsetParent}
if(_4==_3){_5-=isc.Element.getOffsetTop(_2)}
return _5}
,isc.A.getIconTop=function(_1){if(_1==null&&this.icons!=null&&this.icons.getLength()>0)_1=this.icons[0];else if(!this.$145(_1)){this.logWarn("getIconTop() passed invalid icon:"+isc.Log.echoAll(_1));return null}
var _2=this.$16v(_1);if(_2==null){this.logWarn("getIconTop() unable to return position of icon - "+"this icon is not currently drawn into the page. Returning null");return null}
return isc.Element.$s5(_2,this.containerWidget.getClipHandle())}
,isc.A.getPageLeft=function(){return this.getLeft()+((this.containerWidget.getPageLeft()+this.containerWidget.getLeftMargin()+this.containerWidget.getLeftBorderSize())
-this.containerWidget.getScrollLeft())}
,isc.A.getPageTop=function(){return this.getTop()+((this.containerWidget.getPageTop()+this.containerWidget.getTopMargin()+this.containerWidget.getTopBorderSize())
-this.containerWidget.getScrollTop())}
,isc.A.getTitlePageLeft=function(){return this.getTitleLeft()+((this.containerWidget.getPageLeft()+this.containerWidget.getLeftMargin()+this.containerWidget.getLeftBorderSize())
-this.containerWidget.getScrollLeft())}
,isc.A.getTitlePageTop=function(){return this.getTitleTop()+((this.containerWidget.getPageTop()+this.containerWidget.getTopMargin()+this.containerWidget.getTopBorderSize())
-this.containerWidget.getScrollTop())}
,isc.A.getIconRect=function(_1){return[this.getIconLeft(_1),this.getIconTop(_1),this.getIconWidth(_1),this.getIconHeight(_1)]}
,isc.A.getIconPageRect=function(_1){var _2=this.getIconRect(_1);_2[0]+=this.containerWidget.getPageLeft();_2[1]+=this.containerWidget.getPageTop();return _2}
,isc.A.propertyChanged=function(_1,_2){if(this.$14s[_1])this.$18m=true;if(this.$59q[_1])this.updateState();if(_1==this.$59r&&this.items){for(var i=0;i<this.items.length;i++){this.items[i].updateState()}}}
,isc.A.doneSettingProperties=function(){if(this.$18m){var _1=this.form,_2=_1.items;_2.$8j=null;_1.markForRedraw()}
delete this.$18m}
,isc.A.setEditMode=function(_1,_2,_3){if(_1==null)_1=true;if(this.editingOn==_1)return;this.editingOn=_1;if(this.editingOn){this.editContext=_2}
this.editNode=_3}
,isc.A.editClick=function(){var _1=this.getTitlePageLeft(),_2=this.getVisibleTitleWidth(),_3,_4,_5,_6;_4=this.getTitlePageTop();_6=this.getTitleVisibleHeight();_5=this.getVisibleHeight();_3=(_6==_5)?_4:_4+((_6-_5)/2);isc.EditContext.manageTitleEditor(this,_1,_2,_3,null)}
);isc.B._maxIndex=isc.C+378;isc.FormItem.registerStringMethods({showIf:"item,value,form,values",defaultDynamicValue:"item,form,values",focus:"form,item",blur:"form,item",editorEnter:"form,item,value",editorExit:"form,item,value",click:"form,item",doubleClick:"form,item",iconClick:"form,item,icon",iconKeyPress:"keyName,character,form,item,icon",change:"form,item,value,oldValue",changed:"form,item,value",transformInput:"form,item,value,oldValue",cellClick:"form,item",cellDoubleClick:"form,item",titleClick:"form,item",titleDoubleClick:"form,item",mouseMove:"form,item",mouseOver:"form,item",mouseOut:"form,item",titleMove:"form,item",titleOver:"form,item",titleOut:"form,item",itemHover:"item,form",titleHover:"item,form",keyPress:"item, form, keyName, characterValue",keyDown:"item,form,keyName",keyUp:"item,form,keyName",getValueIcon:"value",formSaved:"request,response,data",formatValue:"value,record,form,item",formatEditorValue:"value,record,form,item",parseEditorValue:"value,form,item"});isc.FormItem.getPrototype().toString=function(){return"["+this.Class+" ID:"+this.ID+(this.name!=null?" name:"+this.name:"")+"]"};isc.ClassFactory.defineClass("FormItemFactory");isc.A=isc.FormItemFactory;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$gx="text";isc.A.$18n="Item";isc.A.$18o="TextareaItem";isc.A.$679="TextAreaItem";isc.A.$68a="DatetimeItem";isc.A.$68b="DateTimeItem";isc.A.$18p={};isc.B.push(isc.A.getItemClassName=function(_1,_2,_3,_4){if(_2==null)_2=_1.editorType||_1.formItemType||_1.type;if(isc.isA.String(_2)){var _5=_2.toLowerCase();if(_5.contains(isc.DynamicForm.$12b)){if(_5=="selectother"||_5=="selectotheritem"){_5="select";if(!_4)_1.isSelectOther=true}
if(_5=="select"||_5=="selectitem"){_3=_1.form||_3;var _6=(!isc.ListGrid||_1.multiple||(_3?_3.useNativeSelectItems:false));if(_6)_2="NativeSelectItem"
else _2="SelectItem"}}
if(_5==isc.DynamicForm.$52w)_2="MultiFileItem";else if(_5==isc.DynamicForm.$12g)_2="MultiUploadItem";else if(_5==isc.DynamicForm.$51x.toLowerCase())_2="SOAPUploadItem"}
return _2}
,isc.A.getItemClass=function(_1){var _2=isc.ClassFactory.getClass(_1);if(!_2||!isc.isA.FormItem(_2)){if(_1!=null&&_1.startsWith("T")){var _3=_1.substring(1),_2=isc.ClassFactory.getClass(_3);if(isc.isA.FormItem(_2))return _2}
if(_1==null)_1=this.$gx;var _4=this.$18p,_5=_4[_1];if(!_5){_5=_4[_1]=_1.substring(0,1).toUpperCase()+_1.substring(1)+this.$18n}
if(_5==this.$18o)_5=this.$679;if(_5==this.$68a)_5=this.$68b;_2=isc.ClassFactory.getClass(_5)}
return _2}
,isc.A.makeItem=function(_1){if(_1==null)return null;if(isc.isA.FormItem(_1)){return _1}
var _2=this.getItemClassName(_1),_3=this.getItemClass(_2);if(!_3){this.logWarn("makeItem(): type "+_1.type+" not recognized, using TextItem");_3=isc.TextItem}
return isc.ClassFactory.newInstance(_3,_1)}
);isc.B._maxIndex=isc.C+3;isc.ClassFactory.defineClass("Validator");isc.Validator.addProperties({})
isc.A=isc.Validator;isc.A.notABoolean="Must be a true/false value";isc.A.notAString="Must be a string.";isc.A.notAnInteger="Must be a whole number.";isc.A.notADecimal="Must be a valid decimal.";isc.A.notADate="Must be a date.";isc.A.notATime="Must be a time.";isc.A.notAnIdentifier="Identifiers must start with a letter, underscore or $ character, "+"and may contain only letters, numbers, underscores or $ characters.";isc.A.notARegex="Must be a valid regular expression.";isc.A.notAColor="Must be a CSS color identifier.";isc.A.mustBeLessThan="Must be no more than ${max}";isc.A.mustBeGreaterThan="Must be at least ${min}";isc.A.mustBeLaterThan="Must be later than ${min.toShortDate()}";isc.A.mustBeEarlierThan="Must be earlier than ${max.toShortDate()}";isc.A.mustBeShorterThan="Must be less than ${max} characters";isc.A.mustBeLongerThan="Must be more than ${min} characters";isc.A.mustBeExactLength="Must be exactly ${max} characters";isc.A.notAMeasure='Must be a whole number, percentage, "*" or "auto"';isc.A.requiredField="Field is required";isc.A.notOneOf="Not a valid option";isc.A.notAFunction='Must be a function.';isc.A.$18q="true";isc.A.$18r="false";isc.A.$605=".";isc.A.READONLY="readOnly";isc.A.HIDDEN="hidden";isc.A.DISABLED="disabled";isc.A.$18s={isBoolean:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(isc.isA.Boolean(_3))return true;if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.notABoolean}
if(isc.isA.String(_3)){var _4=isc.Validator;_2.resultingValue=(_3==_4.$18q);return(_3==_4.$18q||_3==_4.$18r)}else if(isc.isA.Number(_3)){_2.resultingValue=(_3!=0);return(_3==0||_3==1)}
_2.resultingValue=!!_3;return false},isString:function(_1,_2,_3){if(_3==null||isc.isA.String(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notAString;_2.resultingValue=isc.iscToLocaleString(_3);return true},isInteger:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notAnInteger;if(isNaN(_3))return false;var _4=parseInt(_3,10),_5=(_3==_4);if(_2.convertToInteger){var _6=parseFloat(_3),_4=Math.round(_6);_2.resultingValue=_4;return true}else{if(_5){_2.resultingValue=_4;return true}else return false}},isFloat:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notADecimal;var _4;if(_3==isc.Validator.$605){_4="0."}else{_4=parseFloat(_3);if(isNaN(_4)||_4!=_3)return false}
_2.resultingValue=_4;return true},isDate:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3)||isc.isA.Date(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notADate;var _4=Date.parseSchemaDate(_3);if(_4==null)return false;_2.resultingValue=_4;return true},isTime:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3)||isc.isA.Date(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notATime;var _4=isc.Time.parseInput(_3,true,true);if(_4==null){_4=Date.parseSchemaDate(_4)}
if(_4!=null){_2.resultingValue=_4;return true}
return false},isIdentifier:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.notAnIdentifier}
return _3.match(/^[a-zA-Z_\$][\w\$]*$/)!=null},isRegexp:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notARegex;if(typeof _3=='object'&&_3.constructor==RegExp)return true;if(isc.Browser.isDOM){if(!isc.Validator.$18t){isc.Validator.$18t=new Function("value","try{var regex=new RegExp(value)}catch(e){return false}return true")}
return isc.Validator.$18t(_3)}else{var _4=new RegExp(_3);return true}},isFunction:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3)||_3==isc.Class.NO_OP||isc.isA.StringMethod(_3))
{return true}
if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notAFunction;try{isc.Func.expressionToFunction("",_3)}catch(e){return false}
if(_3.iscAction)_3=_3.iscAction;_2.resultingValue=isc.StringMethod.create({value:_3});return true},isColor:function(_1,_2,_3){if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notAColor;if(!_3)return true;return isc.isA.color(_3)},isMeasure:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3)||_3=="*")return true;if(!_2.errorMessage)_2.defaultErrorMessage=isc.Validator.notAMeasure;if(isc.isA.String(_3)&&_3.charAt(_3.length-1)=='%'){_3=_3.slice(0,-1);return _3.match(/\d+\.?\d*/)!=null}
return isc.Validator.processValidator(_1,_2,_3,"integerOrAuto")},integerOrAuto:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3)||(isc.isA.String(_3)&&_3.toLowerCase()=="auto"))return true;return isc.Validator.processValidator(_1,_2,_3,"isInteger")},integerRange:function(_1,_2,_3){var _4=_3;if(!isc.isA.String(_3))_3=parseInt(_3,10);if(isNaN(_3)||_3!=_4)return true;_2.dynamicErrorMessageArguments={validator:_2,max:_2.max,min:_2.min}
if(isc.isA.Number(_2.max)&&((!_2.exclusive&&_3>_2.max)||(_2.exclusive&&_3>=_2.max)))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeLessThan}
return false}
if(isc.isA.Number(_2.min)&&((!_2.exclusive&&_3<_2.min)||(_2.exclusive&&_3<=_2.min)))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeGreaterThan}
return false}
return true},lengthRange:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!isc.isA.String(_3))return true;_2.dynamicErrorMessageArguments={validator:_2,max:_2.max,min:_2.min}
var _4=_3.length,_5=_2.max!=null?parseInt(_2.max,10):null,_6=_2.min!=null?parseInt(_2.min,10):null;if(!isc.isA.Number(_5))_5=null;if(!isc.isA.Number(_6))_6=null;if(_5!=null&&_4>_5){_2.defaultErrorMessage=(_5==_6?isc.Validator.mustBeExactLength:isc.Validator.mustBeShorterThan);return false}
if(_6!=null&&_4<_6){_2.defaultErrorMessage=(_5==_6?isc.Validator.mustBeExactLength:isc.Validator.mustBeLongerThan);return false}
return true},contains:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!isc.isA.String(_3))_3=isc.iscToLocaleString(_3);return _3.indexOf(_2.substring)>-1},doesntContain:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!isc.isA.String(_3))_3=isc.iscToLocaleString(_3);return _3.indexOf(_2.substring)==-1},substringCount:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;var _4=_2.substring;for(var _5=0,_6=0;_5<_3.length;_5++){_5=_3.indexOf(_4,_5);if(_5>-1)_6++;else break}
var _7=_2.operator,_8=_2.count;if(!_7)_7="==";if(!_8)_8=0;switch(_7){case"==":return _6==_8;case"!=":return _6!=_8;case"<":return _6<_8;case"<=":return _6<=_8;case">":return _6>_8;case">=":return _6>=_8}
return false},regexp:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;var _4=_2.expression;if(isc.isA.String(_4)){_4=new RegExp(_4)}
return _4.test(_3)},mask:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;var _4=_2.mask;if(isc.isA.String(_4))_4=_2.mask=new RegExp(_4);if(!_4.test(_3)){return false}else{if(_2.transformTo){_2.resultingValue=_3.replace(_4,_2.transformTo)}}
return true},dateRange:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;if(!isc.isA.Date(_3))return false;var _4=_2.min,_5=_2.max;if(_4!=null&&!isc.isA.Date(_4))_4=_2.min=Date.parseSchemaDate(_4);if(_5!=null&&!isc.isA.Date(_5))_5=_2.max=Date.parseSchemaDate(_5);_2.dynamicErrorMessageArguments={validator:_2,max:_5,min:_4}
if(isc.isA.Date(_4)&&((!_2.exclusive&&_3.getTime()<_4.getTime())||(_2.exclusive&&_3.getTime()<=_4.getTime())))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeLaterThan}
return false}
if(isc.isA.Date(_5)&&((!_2.exclusive&&_3.getTime()>_5.getTime())||(_2.exclusive&&_3.getTime()>=_5.getTime())))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeEarlierThan}
return false}
return true},floatLimit:function(_1,_2,_3){var _4;if(_2.precision!=null){if(_2.roundToPrecision==null)_2.roundToPrecision=true;if(!isc.Validator.processValidator(_1,_2,_3,"floatPrecision"))
return false;if(_2.resultingValue!=null)
_3=_4=_2.resultingValue}
if(_2.min!=null||_2.max!=null){if(!isc.Validator.processValidator(_1,_2,_3,"floatRange")){return false}else{if(_4!=null&&_2.resultingValue==null&&_2.roundToPrecision)
_2.resultingValue=_4}}
return true},floatRange:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;var _4=_3;if(!isc.isA.String(_3))_4=parseFloat(_4);if(isNaN(_4)||_4!=_3)return true;_2.dynamicErrorMessageArguments={validator:_2,max:_2.max,min:_2.min}
if(isc.isA.Number(_2.max)&&((!_2.exclusive&&_4>_2.max)||(_2.exclusive&&_4>=_2.max)))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeLessThan}
return false}
if(isc.isA.Number(_2.min)&&((!_2.exclusive&&_4<_2.min)||(_2.exclusive&&_4<=_2.min)))
{if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.mustBeGreaterThan}
return false}
return true},floatPrecision:function(_1,_2,_3){if(_3==null||isc.is.emptyString(_3))return true;var _4=parseFloat(_3);if(isNaN(_4)||_4!=_3)return false;if(isc.isA.Number(_2.precision)){var _5=Math.pow(10,_2.precision);var _6=(Math.round(_4*_5))/_5;if(_2.roundToPrecision){_2.resultingValue=_6;return true}else{return(_4==_6)}
return true}}};isc.A.$74u={requiredIf:{type:"requiredIf",title:"Conditionally required field",condition:function(_1,_2,_3,_4){if(_2.expression!=null&&!isc.isA.Function(_2.expression)){isc.Func.replaceWithMethod(_2,"expression","item,validator,value")}
var _5=_2.expression(_1,_2,_3);if(_2.errorMessage==null)
_2.errorMessage=isc.Validator.requiredField;return!_5||(_3!=null&&!isc.is.emptyString(_3))}},isOneOf:{type:"isOneOf",title:"Is one of list",condition:function(_1,_2,_3,_4){if(_3==null||isc.is.emptyString(_3))return true;var _5=_2.list||(_1?(_1.getValueMap?_1.getValueMap():_1.valueMap):null),_6=_5;if(!isc.isAn.Array(_5)&&isc.isAn.Object(_5)){_6=isc.getKeys(_5)}
if(_6!=null){for(var i=0,_8=_6.length;i<_8;i++){if(_6[i]==_3)return true}}else{isc.Log.logWarn("isOneOf validator specified with no specified list of options "+"or valueMap - validator will always fail. "+"Field definition:"+isc.Log.echo(_1),"validation")}
if(!_2.errorMessage){_2.defaultErrorMessage=isc.Validator.notOneOf}
return false}},required:{type:"required",title:"Required field",defaultErrorMessage:isc.Validator.requiredField,condition:function(_1,_2,_3,_4){return(_3!=null&&!isc.is.emptyString(_3))},action:function(_1,_2,_3,_4){if(!_2.required){_2.$11m=(_1!=null)}}},readOnly:{type:"readOnly",title:"Set field read-only state/appearance",condition:function(_1,_2,_3,_4){return true},action:function(_1,_2,_3,_4){var _5=_2.name;if(_3.fieldAppearance==isc.Validator.HIDDEN){if(_1==true)_4.hideField(_5);else _4.showField(_5)}else if(_3.fieldAppearance==isc.Validator.DISABLED){if(_1==true)_4.disableField(_5);else _4.enableField(_5)}else{if(_1==true)_4.setFieldCanEdit(_5,false);else _4.setfieldCanEdit(_5,true)}}},matchesField:{type:"matchesField",title:"Matches another field value",condition:function(_1,_2,_3,_4){if(_2.otherField==null){isc.logWarn("matchesField validator is missing 'otherField' definition. "+"Validator forced false.");return false}
return(_3==_4[_2.otherField])}},isUnique:{type:"isUnique",title:"Validate field value is unique on DataSource",requiresServer:true},hasRelatedRecord:{type:"hasRelatedRecord",title:"Validate field value exists on a related DataSource",requiresServer:true},serverCustom:{type:"serverCustom",title:"Validate field value using a custom server expression",requiresServer:true}};isc.A=isc.Validator;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.isServerValidator=function(_1){if(_1.serverOnly)return true;var _2=this.$74u[_1.type];if(_2!=null&&_2.requiresServer)return true;return false}
,isc.A.processValidator=function(_1,_2,_3,_4,_5){if(_2.serverOnly)return true;if(!_4)_4=_2.type;var _6=true;var _7;if(_4!=null)_7=this.$74u[_4];var _8;if(_7==null){if(_4!=null)_8=this.$18s[_4];if(_8==null&&_2.condition){if(!isc.isA.Function(_2.condition)){this.logDebug("Creating function for validation condition:\r"+_2.condition);isc.Func.replaceWithMethod(_2,"condition","item,validator,value,record")}
_8=_2.condition}}else{if(_7.requiresServer==true){return true}
_8=_7.condition;if(!_2.errorMessage){_2.defaultErrorMessage=_7.defaultErrorMessage}}
if(_8!=null){if(_1&&_1.multiple&&_1.validateEachItem&&isc.isAn.Array(_3)){var _9=[];for(var i=0;i<_3.length;i++){delete _2.resultingValue;_6=_6&&_8(_1,_2,_3[i],_5);_9[i]=(_2.resultingValue!=null?_2.resultingValue:_3[i])}
_2.resultingValue=_9}else{delete _2.resultingValue;_6=_8(_1,_2,_3,_5)}}else{this.logWarn("validator not understood on item: "+this.echo(_1)+":\r"+isc.Comm.serialize(_2))}
return _6}
,isc.A.performAction=function(_1,_2,_3,_4){var _5=_3.type;var _6;if(_5!=null)_6=this.$74u[_5];var _7;if(_6!=null){_7=_6.action}
if(_7==null&&_3.action){if(!isc.isA.Function(_3.action)){this.logDebug("Creating function for validation action:\r"+_3.action);isc.Func.replaceWithMethod(_3,"action","result,item,validator,component")}
_7=_3.action}
if(_7!=null){_7(_1,_2,_3,_4)}}
,isc.A.getErrorMessage=function(_1){var _2=_1.errorMessage;if(_2==null)_2=_1.defaultErrorMessage;if(_2&&_1.dynamicErrorMessageArguments){_2=_2.evalDynamicString(null,_1.dynamicErrorMessageArguments)}
return _2}
,isc.A.addValidator=function(_1,_2){if(isc.isA.String(_1)){var _3={};_3[_1]=_2;return this.addValidators(_3)}}
,isc.A.addValidators=function(_1){for(var _2 in _1){if(!isc.isA.Function(_1[_2])){isc.Func.replaceWithMethod(_1,_2,"item,validator,value")}}
isc.addMethods(this.$18s,_1)}
,isc.A.addValidatorDefinition=function(_1,_2){if(!isc.isAn.Object(_2)){isc.logWarn("Invalid validator in call to addValidatorDefinition. Ignored.")}
var _3={};_3[_1]=_2;return this.addValidatorDefinitions(_3)}
,isc.A.addValidatorDefinitions=function(_1){if(!_1||!isc.isAn.Object(_1))return;for(var _2 in _1){if(this.$74v[_2]){isc.logWarn("addValidatorDefinitions: Validator definition already exists "+"for type "+_2+". Replacing.")}}
isc.addProperties(this.$74u,_1)}
);isc.B._maxIndex=isc.C+8;isc.ClassFactory.defineClass("ContainerItem","FormItem");isc.A=isc.ContainerItem.getPrototype();isc.A.cellSpacing=0;isc.A.cellPadding=2;isc.A.cellBorder=0;isc.A.recalculateItemsOnRedraw=false;isc.A.$125=false;isc.A.changeOnKeypress=false;isc.ContainerItem.addMethods(isc.applyMask(isc.DynamicForm.getPrototype(),["getTableStartHTML","$11n","$11o","getCellStartHTML","$11g","getCellEndHTML","$11h","getTitleAlign","getItemPromptHTML","getItem"]))
isc.A=isc.ContainerItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$18u=isc.Canvas.$42a;isc.B.push(isc.A.init=function(){this.Super("init",arguments);this.setItems(this.items?this.items:null)}
,isc.A.destroy=function(){this.Super("destroy",arguments);if(this.items){for(var i=0;i<this.items.length;i++){this.items[i].destroy()}}}
,isc.A.setItems=function(_1){var _2=this.items?this.items:null;if(_1)this.items=_1;else _1=this.items;if(!this.items)return null;this.logDebug("Creating "+this.items.length+" contained items");var _3=false;for(var _4=0;_4<_1.length;_4++){var _5=_1[_4];if(!_5){_1.removeItem(_4--);continue}
isc.addMethods(_5,{$154:function(){return this.parentItem.$154()}});_5.containerWidget=this.containerWidget;_5.parentItem=this;_5.eventParent=this;_5.form=this.form;_5.showTitle=false;if(!isc.isA.FormItem(_5))_1[_4]=_5=isc.FormItemFactory.makeItem(_5);if(this.accessKey!=null&&!_3&&_5.$kk()){_5.accessKey=this.accessKey;_3=true}
if(_5.name!=null)this[_5.name]=_5}
if(this.isDrawn()){if(_2&&_2!=this.items){this.$60r={};for(var i=0;i<_2.length;i++){var _7=_2[i];if(!_1.contains(_2[i])){this.$60r[_2[i].getID()]=true}}}}
this.redraw()}
,isc.A.getTitleHTML=function(){var _1=this.getTitle();if(this.accessKey!=null){_1=isc.Canvas.hiliteCharacter(_1,this.accessKey)}
return _1}
,isc.A.$173=function(_1){if(!this.isVisible()||!this.containerWidget.isDrawn())return;this.$174=_1;for(var i=0;i<this.items.length;i++){if(this.items[i].$kk())this.items[i].$173(_1)}
if(!this.form.isDirty()){this.$175()}}
,isc.A.isEditable=function(){return false}
,isc.A.$kk=function(){if(!this.items)return false;for(var i=0;i<this.items.length;i++){if(this.items[i].$kk())return true}
return false}
,isc.A.focusInItem=function(){if(!this.isVisible()||!this.$kk())return;for(var i=0;i<this.items.length;i++){if(this.items[i].$kk()){this.items[i].focusInItem();break}}}
,isc.A.blurItem=function(){for(var i=0;i<this.items.length;i++){if(this.items[i].hasFocus){this.items[i].blurItem();break}}}
,isc.A.$159=function(){this.$16c()}
,isc.A.drawn=function(){var _1=this.items;if(!_1)return;for(var i=0;i<_1.length;i++){if(_1[i].visible!=false)_1[i].drawn()}
return this.Super("drawn",arguments)}
,isc.A.redrawn=function(){var _1=this.items;if(!_1)return;for(var i=0;i<_1.length;i++){var _3=_1[i];if(_3.visible!=false){if(!_3.isDrawn())_3.drawn();else _3.redrawn()}else{if(_3.isDrawn())_3.cleared()}}
if(this.$60r){for(var _4 in this.$60r){if(window[_4]!=null)window[_4].cleared()}
delete this.$60r}
return this.Super("redrawn",arguments)}
,isc.A.cleared=function(){var _1=this.items;if(!_1)return;for(var i=0;i<_1.length;i++){if(_1[i].isDrawn())_1[i].cleared()}
if(this.$60r){for(var _3 in this.$60r){if(window[_3]!=null)window[_3].cleared()}
delete this.$60r}
return this.Super("cleared",arguments)}
,isc.A.makeNamedItem=function(_1,_2){if(!this.itemCache)this.itemCache={};var _3=this.itemCache[_1];if(!_3){_3=(this[_1]||this.getClass()[_1]);if(_2!=null){_3=isc.addProperties({},_3,_2)}
_3=this.itemCache[_1]=isc.FormItemFactory.makeItem(_3)}
return _3}
,isc.A.getInnerHTML=function(_1,_2,_3,_4){if(!_1)_1={};if(!this.items||this.recalculateItemsOnRedraw||!isc.isA.FormItem(this.items[0]))this.setItems();if(!this.items)return"No items set for containerItem "+this;var _5;if(this.isInactiveHTML()&&this.$68u==null){_5=true;this.$68u=this.setupInactiveContext(null);if(this.logIsDebugEnabled("inactiveEditorHTML")){this.logDebug("getInnerHTML(): Item is marked as inactive - set up "+"new inactive context ID:"+this.$68u,"inactiveEditorHTML")}}
var _6=this.getErrorOrientation(),_7,_8=_6==isc.Canvas.LEFT,_9;if(_3&&(_8||_6==isc.Canvas.RIGHT))
{var _10=this.getErrors();if(_10){_7=true;_9=this.getErrorHTML(_10)}}
var _11=isc.StringBuffer.newInstance();if(this.$11d()){_11.append(this.$15l())}
_11.append(this.getTableStartHTML());var _12=this.items;for(var _13=0;_13<_12.length;_13++){var _14=_12[_13];_14.$10x()
_14.form=this.form;if(_14.showIf){if(!isc.isA.Function(_14.showIf)){isc.Func.replaceWithMethod(_14,"showIf","item,value,form")}
var _15=this.getItemValue(_14,_1);var _16=(_14.showIf(_14,_15,this.form)!=false);if(_16!=_14.visible){_14.visible=_16}}}
for(var _13=0,_17=this.items.length;_13<_17;_13++){var _14=this.items[_13];if(!_14)continue;if(!_14.visible)continue;var _15=this.getItemValue(_14,_1);if(_14.$8l||_13==0){if(_13!=0)_11.append("</TR>");_11.append("<TR>")}
if(_13==0&&_7&&_8){var _18=1;for(var _19=1;_19<this.items.length;_19++){if(this.items[_19].$8l)_18++}
_11.append("<TD ROWSPAN=",_18,">",_9,"</TD>")}
_11.append(this.getCellStartHTML(_14));_11.append(_14.getInnerHTML(_15,true));_11.append(this.getCellEndHTML(_14))}
if(this.showPickerIcon||(this.showIcons&&this.icons!=null)){var _20=this.getTotalIconsWidth();if(this.showPickerIcon)_20+=this.getPickerIconWidth();_11.append(this.$11g((this.form.isRTL()?isc.Canvas.RIGHT:isc.Canvas.LEFT),this.getCellStyle(),1,1,_20,null,null,this.$18u,null,null,null,(this.icons&&(this.showPickerIcon||this.icons.length>1))));if(this.showPickerIcon)_11.append(this.getIconHTML(this.getPickerIcon()));_11.append(this.getIconsHTML());_11.append(this.$11h(true))}
if(_7&&!_8)_2=true;if(_2){var _21=this.getHint(),_22=!_8?_9:null,_23=(_21&&_22)?_21+_22:(_21||_22);if(_23&&!isc.isA.emptyString(_23)){this.$132[1]=this.$59s();this.$132[3]=this.getHintStyle();this.$132[5]=_23;_11.append(this.$132)}}
_11.append("</TR></TABLE>");if(_5)delete this.$68u;return _11.toString()}
,isc.A.getItemValue=function(_1,_2){if(_2==null)_2={};if(!isc.isA.FormItem(_1))_1=this.getItem(_1);if(!_1)return null;var _3=_1.getFieldName(),_4=null;if(_1.value!=null)_4=_1.value;if(_4==null&&_3){_4=_2[_3]}
if(_4==null){_4=_1.getDefaultValue();if(_4==null&&this.form&&this.form.values)_4=this.form.values[_3]}
return _4}
,isc.A.$10v=function(){if(this.items==null)return false;for(var i=0;i<this.items.length;i++){if(this.items[i].$10v())return true}
return this.$18j}
,isc.A.$10x=function(){this.$18j=false;for(var i=0;i<this.items.length;i++){this.items[i].$10x()}}
,isc.A.updateDisabled=function(){this.Super("updateDisabled",arguments);if(this.items){for(var i=0;i<this.items.length;i++)this.items[i].updateDisabled()}}
,isc.A.getTextDirection=function(){return this.form.getTextDirection()}
,isc.A.getLeft=function(){var _1=this.$11o();if(_1==null){this.logWarn("getLeft() Unable to determine position for "+(this.name==null?"this item ":this.name)+". Position cannot be determined before the item is drawn "+"- returning zero");return 0}
return this.$18k(_1)}
,isc.A.getTop=function(){var _1=this.$11o();if(_1==null){this.logWarn("getTop() Unable to determine position for "+(this.name==null?"this item ":this.name)+". Position cannot be determined before the item is drawn "+"- returning zero");return 0}
return this.$18l(_1)}
,isc.A.getVisibleWidth=function(){var _1=this.$11o();if(_1==null)return this.Super("getVisibleWidth",arguments);return _1.offsetWidth}
,isc.A.getVisibleHeight=function(){var _1=this.$11o();if(_1==null)return this.Super("getVisibleHeight",arguments);return _1.offsetHeight}
);isc.B._maxIndex=isc.C+24;isc.ClassFactory.defineClass("CanvasItem","FormItem");isc.A=isc.CanvasItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.width="*";isc.A.height=null;isc.A.shouldSaveValue=false;isc.B.push(isc.A.$18v=function(_1,_2,_3){this.Super("resized",arguments);if(!this.dragResizing()){this.canvasItem.canvasResized(_1,_2,_3)}}
,isc.A.$18w=function(){this.canvasItem.canvasResized(1,1);return this.Super("dragResized",arguments)}
,isc.A.$18x=function(_1,_2){return this.canvasItem.form.$kf(_1,_2)}
);isc.B._maxIndex=isc.C+3;isc.A=isc.CanvasItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.Super("init",arguments);this.$18y()}
,isc.A.isEditable=function(){return false}
,isc.A.$kk=function(){var _1=this.canvas;if(_1&&_1.$kk)return _1.$kk();return this.canFocus||false}
,isc.A.$18y=function(){if(!isc.isAn.Object(this.canvas)&&!this.canvasProperties&&!window[this.canvas]){if(isc.designTime)return;this.logWarn("CanvasItem: "+(this.getFieldName()?this.getFieldName():this.getID())+" defined with no canvas property - creating a default "+"canvas for this item.")}
if(!isc.isAn.Object(this.canvas)&&isc.isA.Canvas(window[this.canvas])){this.canvas=window[this.canvas]}
var _1={_redrawWithParent:false,$so:true,tabIndex:-1,canvasItem:this,disabled:this.isDisabled()};_1.resized=this.$18v;_1.dragResized=this.$18w;_1.$kf=this.$18x;if(this.dataSource)_1.dataSource=this.dataSource;if(this.prompt)_1.prompt=this.prompt;if(this.accessKey!=null)_1.accessKey=this.accessKey;if(this.showFocused!=null)_1.showFocused=this.showFocused;if(this.showFocusedAsOver!=null)_1.showFocusedAsOver=this.showFocusedAsOver;if(isc.isA.String(this.canvas)&&window[this.canvas])this.canvas=window[this.canvas];if(!isc.isA.Canvas(this.canvas)){isc.addProperties(_1,this.canvas);if(_1.ID==null)_1.ID=null;this.autoDestroy=true;this.addAutoChild("canvas",_1,isc.Canvas,this.containerWidget)}else{this.canvas.setTabIndex(-1);this.canvas.setPrompt(this.prompt);this.canvas.setAccessKey(this.accessKey);isc.addProperties(this.canvas,_1);if(_1.dataSource)this.canvas.bindToDataSource();this.canvas.setDisabled(this.isDisabled());this.containerWidget.addChild(this.canvas)}}
,isc.A.setCanvas=function(_1){if(_1)this.canvas=_1;this.$18y()}
,isc.A.destroy=function(){if(this.canvas){delete this.canvas.canvasItem;if(this.autoDestroy)this.canvas.destroy(true);else if(this.canvas.visibility!=isc.Canvas.HIDDEN)this.canvas.hide()}
return this.Super("destroy",arguments)}
,isc.A.placeCanvas=function(_1){var _2=this.canvas;if(this.visible==false){_2.hide();_2.moveTo(0,0);return}
if(this.form&&!this.form.isDrawn()&&this.form.position==isc.Canvas.RELATIVE){_2.hide();return}
var _3=this.containerWidget.getClipHandle(),_4=isc.Element.get(this.getID()+"$18z");var _5=isc.Element.$s4(_4,_3),_6=isc.Element.$s5(_4,_3);_2.moveTo(_5,_6);if(_2.visibility==isc.Canvas.HIDDEN){_2.show()}
if(!_1&&isc.Browser.isMac&&isc.Browser.isMoz&&_5==0&&_6==0){isc.Timer.setTimeout({target:this,methodName:"$180"},0)}}
,isc.A.$180=function(){this.placeCanvas(true)}
,isc.A.cleared=function(){this.Super("cleared",arguments);if(this.canvas&&this.canvas.isDrawn())this.canvas.clear()}
,isc.A.moved=function(){if(this.isDrawn())this.placeCanvas()}
,isc.A.checkCanvasOverflow=function(){return this.sizeCanvas(true)}
,isc.A.sizeCanvas=function(_1){var _2=this.canvas;if(_1&&!(_2.overflow==isc.Canvas.VISIBLE||_2.overflow==isc.Canvas.CLIP_H))
{this.logDebug("ignoring first pass, can't overflow","canvasItemSizing");return}
var _3=this.getInnerWidth(),_4=this.getInnerHeight(),_5,_6;if(this.showTitle&&this.getTitleOrientation()==isc.Canvas.TOP){_4-=this.form.getTitleHeight(this)}
_6=_4;var _7=_2.$pn||this.width;_5=(_7==null||_7=="*"?_3:_7);if(!_2.isDirty()&&(_5==null||_5<=_2.getVisibleWidth())&&_2.getHeight()<_2.getVisibleHeight()&&_6<=_2.getVisibleHeight())
{this.logDebug("not applying height: "+_6+" to overflowed Canvas with height: "+_2.getVisibleHeight(),"canvasItemSizing");_6=null}
if(!isc.isA.Number(_5))_5=null;if(!isc.isA.Number(_6))_6=null;this.$602(_5,_6);this.logDebug("this.$8t: "+this.$8t+", policy size: "+[_3,_4]+", specifiedSize: "+[_7,_2.$po||this.height]+", Resized Canvas to: "+[_5,_6],"canvasItemSizing");if(!_2.isDrawn()){var _8=this.containerWidget.getDrawnState();if(_8==isc.Canvas.COMPLETE||_8==isc.Canvas.HANDLE_DRAWN){_2.draw()}}else _2.redrawIfDirty("CanvasItem getting new size");var _9=_2.getVisibleWidth(),_10=_2.getVisibleHeight();this.logDebug("visible size of embedded Canvas: "+[_9,_10],"canvasItemSizing");if(!_1)this.minHeight=null;else this.minHeight=_10>_2.getHeight()?_10:null;if(_10>_4)return true}
,isc.A.$602=function(_1,_2){this.$181=true;this.canvas.resizeTo(_1,_2);this.$181=false}
,isc.A.getElementHTML=function(_1){var _2=this.canvas;if(_2&&this.$68y()){return _2.getPrintHTML(this.containerWidget.currentPrintProperties)}
this.sizeCanvas();this.$173(this.getGlobalTabIndex());return"<SPAN style='padding:0px;margin:0px;' ID='"+this.getID()+"$18z'>"+isc.Canvas.spacerHTML(_2.getVisibleWidth(),_2.getVisibleHeight())+"</SPAN>"}
,isc.A.$159=function(){this.$16c()}
,isc.A.getHeight=function(_1){var _2=this.canvas;if(_1){var _3=_2.getVisibleHeight();if(_3>_2.getHeight())return _3}
return this.canvas.$po||this.height||this.canvas.defaultHeight}
,isc.A.getWidth=function(){return this.canvas.$pn||this.width||this.canvas.defaultWidth}
,isc.A.canvasResized=function(_1,_2,_3){if(this.$181)return;var _4=this.canvas,_5=_4.getWidth(),_6=_4.getHeight();if(!_4.isDrawn())return;if(_3!="overflow"){if(_1!=null&&_1!=0)_4.$pn=_5;if(_2!=null&&_2!=0)_4.$po=_6}
this.logDebug("canvas resized: new specified sizes: "+[_5,_6],"canvasItemSizing");this.redraw()}
,isc.A.updateDisabled=function(){this.Super("updateDisabled",arguments);this.canvas.setDisabled(this.isDisabled())}
,isc.A.$173=function(_1){this.$182(_1)}
,isc.A.$182=function(_1){var _2=this.canvas;if(_2){_2.$rp();_2.$vk(_1,false)}}
,isc.A.focusInItem=function(){if(this.canvas)this.canvas.focus();return this.Super("focusInItem",arguments)}
,isc.A.blurItem=function(){if(this.canvas)this.canvas.blur();return this.Super("blurItem",arguments)}
);isc.B._maxIndex=isc.C+23;isc.ClassFactory.defineClass("TextItem","FormItem");isc.A=isc.TextItem;isc.A.DEFAULT="default";isc.A.UPPER="upper";isc.A.LOWER="lower";isc.A.$713={'0':{charFilter:"[0-9+\\-]"},'#':{charFilter:"[0-9]"},'9':{charFilter:"[0-9 ]"},'L':{charFilter:"[A-Za-z]"},'?':{charFilter:"[A-Za-z ]"},'a':{charFilter:"[0-9A-Za-z]"},'A':{charFilter:"[0-9A-Za-z]"},'C':{charFilter:"."}};isc.A=isc.TextItem.getPrototype();isc.A.width=150;isc.A.height=isc.Browser.isSafari?22:19;isc.A.textBoxStyle="textItem";isc.A.length=null;isc.A.canAutoComplete=true;isc.A.$183="TEXT";isc.A.$125=true;isc.A.$15i=true;isc.A.$185=null;isc.A.redrawOnShowIcon=false;isc.A.$16b={onmousedown:(isc.Browser.isIE?function(){var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3)_3.$179()}:null)};isc.A=isc.TextItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$186=[,"<INPUT TYPE=",," NAME='",,"' ID='",,"' handleNativeEvents=false"];isc.A.$14f=" TABINDEX=";isc.A.$oa=">";isc.A.$pi=" DISABLED ";isc.A.$187="native";isc.A.$188=" AUTOCOMPLETE=OFF ";isc.A.$14e=" ACCESSKEY=";isc.A.$19a=[" CLASS='",,"' STYLE='",,,,,,,,,,,,,,,,(isc.Browser.isMoz?"-moz-user-focus:":isc.Browser.isIE?"margin-top:-1px;margin-bottom:-1px;":null),,"' "];isc.A.$93="WIDTH:";isc.A.$38="px;";isc.A.$92="HEIGHT:";isc.A.$14c="text-align:";isc.A.$39=";";isc.A.$19b="normal;";isc.A.$19c="ignore;";isc.A.applyStaticTypeFormat=false;isc.A.characterCasing=isc.TextItem.DEFAULT;isc.A.maskPadChar=" ";isc.A.maskPromptChar="_";isc.B.push(isc.A.$43h=function(){if(this.changeOnKeypress)this.$18c()}
,isc.A.$429=function(){return!isc.Browser.isIE}
,isc.A.getTextBoxCellCSS=function(){return this.$136}
,isc.A.getElementHTML=function(_1,_2){var _3=this.$xq(_2);if(this.showValueIconOnly)return _3;var _4=this.$186,_5=this.form,_6=_5.getID(),_7=this.getItemID();_4[0]=_3;_4[2]=this.$183;_4[4]=this.getElementName();_4[6]=this.getDataElementId();_4[8]=this.$155();if(isc.Browser.isMoz||isc.Browser.isSafari){if(this.getBrowserSpellCheck())_4[_4.length]=" spellcheck=true";else _4[_4.length]=" spellcheck=false"}
if(this.$429){_4[_4.length]=" ONINPUT='"
_4[_4.length]=this.getID()
_4[_4.length]=".$43g()'"}
if(this.isDisabled())_4[_4.length]=this.$pi;if(this.isInactiveHTML()||this.readOnly){_4[_4.length]=" READONLY=TRUE"}
if(this.isInactiveHTML()&&_1!=null&&_1!=isc.emptyString){_4[_4.length]=" value='"+_1+"'"}
if(this.$17h()!=this.$187){_4[_4.length]=this.$188}
_4[_4.length]=this.getElementStyleHTML();var _8=this.$154();if(_8!=null){var _9=_4.length;_4[_9]=this.$14f;isc.$bk(_4,_8,_9+1,5)}
if(this.showTitle==false&&this.accessKey!=null){_4[_4.length]=this.$14e;_4[_4.length]=this.accessKey}
_4[_4.length]=this.$oa;var _10=_4.join(isc.emptyString);_4.length=8;return _10}
,isc.A.$143=function(){return isc.Browser.isStrict}
,isc.A.$12x=function(_1,_2){var _3=this.Super("$12x",arguments);if(this.changeOnBlur)this.form.elementChanged(this);else{var _4=this.getElementValue();if(this.mapDisplayToValue){_4=this.mapDisplayToValue(_4)}
if(this._value!=_4)this.form.elementChanged(this)}
if(this.mask){var _5=this.getValue();if(this.mapValueToDisplay){_5=this.mapValueToDisplay(_5)}
this.setElementValue(_5)}
if(this.showHintInField){var _6;var _5=this.getElementValue();if(_5===_6||_5==null||isc.is.emptyString(_5)){this.$66t()}}
return _3}
,isc.A.getElementStyleHTML=function(){var _1=this.$19a,_2=this.getTextBoxWidth(),_3=this.getTextBoxHeight(),_4=this.getTextBoxStyle();_1[1]=_4;if(isc.isA.Number(_2)){_1[3]=this.$93;isc.$bk(_1,_2,4,4);_1[8]=this.$38}else{_1[3]=_1[4]=_1[5]=_1[6]=_1[7]=_1[8]=null}
if(isc.isA.Number(_3)){_1[9]=this.$92;isc.$bk(_1,_3,10,4);_1[14]=this.$38}else{_1[9]=_1[10]=_1[11]=_1[12]=_1[13]=_1[14]=null}
if(this.textAlign){_1[15]=this.$14c;_1[16]=this.textAlign;_1[17]=this.$39}else{_1[15]=_1[16]=_1[17]=null}
if(isc.Browser.isMoz){_1[19]=(this.$154()>0?this.$19b:this.$19c)}
return _1.join(isc.emptyString)}
,isc.A.mapValueToDisplay=function(_1){if(this.mask){var x=this.$715();if(!this.hasFocus)
x=this.$714(_1);return x}
var _3=isc.FormItem.$b4.mapValueToDisplay.call(this,_1);if(_3==null)return isc.emptyString;return _3}
,isc.A.mapDisplayToValue=function(_1){if(this.mask){var _2=this.$716(_1)}else{_2=this.$17d(_1)}
if(!this.applyStaticTypeFormat&&this.parseEditorValue!=null){return this.parseEditorValue(_1,this.form,this)}
if(isc.is.emptyString(_2))_2=this.$185;return _2}
,isc.A.saveValue=function(_1,_2){if(this.mask)this.$714(_1);this.Super("saveValue",arguments)}
,isc.A.setValue=function(_1,_2,_3,_4){this.$66s();var _5;if(_1!==_5&&(_1==null||isc.is.emptyString(_1)))
this.$185=_1;if(_1!==_5&&_1!=null&&this.characterCasing!=isc.TextItem.DEFAULT){if(this.characterCasing==isc.TextItem.UPPER){_1=_1.toUpperCase()}else if(this.characterCasing==isc.TextItem.LOWER){_1=_1.toLowerCase()}}
_1=this.invokeSuper(isc.TextItem,"setValue",_1,_2,_3,_4);if(!this.hasFocus&&this.showHint&&this.showHintInField&&this.getHint()){if(_1===_5||_1==null||isc.is.emptyString(_1)){this.$66t()}}
return _1}
,isc.A.getCriteriaFieldName=function(){if(this.displayField)return this.displayField;return this.getFieldName()}
,isc.A.$12v=function(_1,_2){var _3=this.Super("$12v",arguments);this.$66s();if(this.mask){this.$717(false);var _4=0;var _5=this.$718;if(!this.selectOnFocus){_4=this.$719();_5=_4}
this.delayCall("$72a",[_4,_5])}
return _3}
,isc.A.setKeyPressFilter=function(_1){if(this.mask){this.logWarn("setKeyPressFilter() ignored because mask is enabled");return}
this.keyPressFilter=_1;this.$66y=null;if(this.keyPressFilter){this.$66y=new RegExp(this.keyPressFilter)}}
,isc.A.init=function(){this.Super("init",arguments);if(this.mask&&!isc.isA.ComboBoxItem(this)&&!isc.isA.SpinnerItem(this)){this.$72b();if(this.keyPressFilter){this.logWarn("init: keyPressFilter ignored because mask is enabled")}}else if(this.keyPressFilter){this.$66y=new RegExp(this.keyPressFilter)}}
,isc.A.keyPress=function(_1,_2,_3,_4){if(isc.EventHandler.ctrlKeyDown()||isc.EventHandler.altKeyDown())return true;if((!this.characterCasing||this.characterCasing==isc.TextItem.DEFAULT)&&!this.$66y&&!this.mask)
{return true}
if(this.mask){var _5=this.$72c();var _6=isc.Browser.isSafari;var _7=_5.begin;if(_3=="Backspace"||_3=="Delete"){if((_5.begin-_5.end)!=0||(_6&&this.$72d))
{if(isc.Browser.isSafari&&this.$72d){_5=this.$72d;this.$72d=null}
if(this.maskOverwriteMode){this.$72e(_5.begin,_5.end)}else{var _8=_5.end-_5.begin;this.$72f(_5.begin,_8)}
this.$717(true);this.$73t(_5.begin,0)}else{if(_3=="Backspace"){var _9=((_6&&_5.begin==_5.end)?_7:_7-1);if(_9>=0){if(this.maskOverwriteMode){while(!this.$72i[_9]&&_9>=0)_9--;this.$72l[_9]=this.maskPromptChar}else{this.$72f(_9)}
this.$717(true);this.$73t(_9,-1)}}else{if(this.maskOverwriteMode){this.$72l[_7]=this.maskPromptChar}else{this.$72f(_7)}
this.$717(true);this.$73t(_7,0)}}
return false}
if(this.$660(_4)&&((_5.begin-_5.end)!=0||(_6&&this.$72d)))
{if(isc.Browser.isSafari&&this.$72d){_5=this.$72d;this.$72d=null}
if(this.maskOverwriteMode){this.$72e(_5.begin,_5.end)}else{var _8=_5.end-_5.begin;this.$72f(_5.begin,_8)}}
if(_6&&(_5.begin-_5.end)!=0&&!this.$660(_4))
{this.$72d=_5}else{this.$72d=null}
if(_3=="Escape"){this.$72e(0,this.$718)
this.$717(true);this.$72a(this.$72g);return false}}
if((this.mask&&!this.$660(_4))||(!this.mask&&((!this.$66y&&!this.$66z(_4))||(this.$66y&&!this.$660(_4)))))
{return true}
var c=String.fromCharCode(_4);if(this.mask){var p=this.$72h(_7-1);if(p<this.$718){var _12=this.$72i[p];if(_12){if(_12.casing){c=this.$72j(c,_12.casing)}
if(_12.filter.test(c)){if(!this.maskOverwriteMode)
this.$72k(p);this.$72l[p]=c;var _13=p;if(this.$717(true)){_13=this.$72h(p)}
this.$72a(_13)}}}
return false}
var _14=c;if(!this.mask)_14=this.$72j(c,this.characterCasing);if(c==_14&&!this.$66y)return true;if(this.$66y){if(this.$660(_4)&&!this.$66y.test(_14)){return false}}
if(c==_14)return true;var _15=this.getValue()||"";var _5=this.getSelectionRange();if((_5[0]-_5[1])!=0){_15=_15.substring(0,_5[0])+_14+_15.substring(_5[1]+1)}else{_15=_15.substring(0,_5[0])+_14+_15.substring(_5[1])}
this.setValue(_15);this.setSelectionRange(_5[0]+1,_5[0]+1);return false}
,isc.A.$660=function(_1){return((_1>=32&&_1<=126)||_1>127)}
,isc.A.$66z=function(_1){return(_1>=65&&_1<=90)||(_1>=97&&_1<=122)}
,isc.A.$72j=function(_1,_2){if(_2==isc.TextItem.UPPER){_1=_1.toUpperCase()}else if(_2==isc.TextItem.LOWER){_1=_1.toLowerCase()}
return _1}
,isc.A.setMask=function(_1){if(isc.isA.ComboBoxItem(this)||isc.isA.SpinnerItem(this)){return}
this.mask=_1;this.$72b();if(this.keyPressFilter){this.$66y=null;this.logWarn("setMask: keyPressFilter ignored because mask is enabled")}
this.setValue("")}
,isc.A.$72b=function(){this.$72i=[];this.$72l=[];this.$718=0;var _1=null;var _2=false;var _3=false;var _4="";var _5=this.mask.split("");for(var i=0;i<_5.length;i++){var c=_5[i];if(c=="<"){_1=(_1==isc.TextItem.LOWER?null:isc.TextItem.LOWER)}else if(c==">"){_1=(_1==isc.TextItem.UPPER?null:isc.TextItem.UPPER)}else{if(!_2&&c=="\\"){_2=true}else if(_2){this.$72m(c,_1);_2=false}else{if(!_3&&c=="["){_3=true;_4+=c}else if(_3&&c=="]"){_3=false;_4+=c;this.$72i.push({filter:new RegExp(_4),casing:_1});if(this.$72g==null){this.$72g=this.$72i.length-1}
this.$72l.push(this.maskPromptChar);this.$718++;_4=""}else if(_3){_4+=c}else{this.$72n(c,_1)}}}}}
,isc.A.$72m=function(_1,_2){this.$72i.push(null);this.$72l.push(_1);this.$718++}
,isc.A.$72n=function(_1,_2){var _3=isc.TextItem.$713[_1];if(_3){this.$72i.push({filter:new RegExp(_3.charFilter),casing:_2});if(this.$72g==null){this.$72g=this.$72i.length-1}
this.$72l.push(this.maskPromptChar)}else{this.$72i.push(null);this.$72l.push(_1)}
this.$718++}
,isc.A.$72c=function(){var _1=this.getSelectionRange();return{begin:_1[0],end:_1[1]}}
,isc.A.$72a=function(_1,_2){if(this.hasFocus){_2=(isc.isA.Number(_2)?_2:_1);this.setSelectionRange(_1,_2)}}
,isc.A.$72h=function(_1){while(++_1<this.$718){if(this.$72i[_1])return _1}
return this.$718}
,isc.A.$719=function(){var _1=0;for(var i=this.$718-1;i>=0;i--){if(this.$72i[i]){if(this.$72l[i]==this.maskPromptChar)
_1=i;else
break}}
return _1}
,isc.A.$714=function(_1){if(_1==null)_1="";this.$72e(0,this.$718);var _2=-1;if(this.maskSaveLiterals){for(var i=0,_4=0;i<_1.length;i++){if(this.$72i[i]){var c=_1.charAt(i);if(c==" "){if(!this.hasFocus)
this.$72l[i]=c}else if(this.$72i[i].filter.test(c)){this.$72l[i]=c;_2=i}}}}else{for(var i=0,_4=0;i<this.$718;i++){if(this.$72i[i]){while(_4<_1.length){var c=_1.charAt(_4++);if(c==" "){if(!this.hasFocus)this.$72l[i]=c;break}else if(this.$72i[i].filter.test(c)){this.$72l[i]=c;_2=i;break}}
if(_4>_1.length)break}}}
_1=this.$715();if(!this.hasFocus){if(_2>=0){for(var i=_2+1;i<this.$718;i++){if(this.$72i[i])break;_2++}}
_1=_1.substring(0,_2+1)}
return _1}
,isc.A.$716=function(_1){if(_1==null)_1="";var _2=false;var _3=-1;var _4="";for(var i=0,_6=0;i<_1.length;i++){var c=_1.charAt(i);if(this.$72i[i]){if(c!=this.maskPromptChar&&this.$72i[i].filter.test(c)){_4+=c;_2=true;_3=_6++}else{_4+=this.maskPadChar;_6++}}else if(this.maskSaveLiterals){_4+=c;_3=_6++}}
if(!_2){_4=""}else{_4=_4.substring(0,_3+1)}
return _4}
,isc.A.$715=function(){return this.$72l.join('')}
,isc.A.$72e=function(_1,_2){for(var i=_1;i<_2&&i<this.$718;i++){if(this.$72i[i])this.$72l[i]=this.maskPromptChar}}
,isc.A.$717=function(_1){var _2=this.$715();this.setElementValue(_2);if(_1&&this.changeOnKeypress){var _3=this.$716(_2);var _4=this.handleChange(_3,this._value);if(this.destroyed)return;_3=this.$17n;this.updateAppearance(_3);this.saveValue(_3);this.handleChanged(_3);return _4}
return true}
,isc.A.$73t=function(_1,_2){if(_2<0){while(!this.$72i[_1]&&_1>=0)_1--}else{while(!this.$72i[_1]&&_1<this.$718)_1++}
this.$72a(_1)}
,isc.A.$72f=function(_1,_2){if(!_2)_2=1;while(!this.$72i[_1]&&_1>=0)_1--;for(var i=_1,_4=i+_2-1;i<this.$718;i++){if(this.$72i[i]){this.$72l[i]=this.maskPromptChar;var j=this.$72h(_4++);var _6=this.$72i[i];var c=this.$72l[j];if(j<this.$718&&_6.filter.test(c)){if(_6.casing){c=this.$72j(c,_6.casing)}
this.$72l[i]=c}else{while(i<j){if(this.$72i[i])this.$72l[i]=this.maskPromptChar;i++}
break}}}}
,isc.A.$72k=function(_1){for(var i=_1,c=this.maskPromptChar;i<this.$718;i++){var _4=this.$72i[i];if(_4){if(_4.casing){c=this.$72j(c,_4.casing)}
var j=this.$72h(i);var t=this.$72l[i];this.$72l[i]=c;if(j<this.$718&&this.$72i[j].filter.test(t)){c=t}else{break}}}}
);isc.B._maxIndex=isc.C+35;isc.ClassFactory.defineClass("BlurbItem","FormItem");isc.A=isc.BlurbItem.getPrototype();isc.A.shouldSaveValue=false;isc.A.height=null;isc.A.showTitle=false;isc.A.colSpan="*";isc.A.startRow=true;isc.A.endRow=true;isc.A.textBoxStyle="staticTextItem";isc.A.emptyDisplayValue="&nbsp;";isc.ClassFactory.defineClass("ButtonItem","CanvasItem");isc.A=isc.ButtonItem.getPrototype();isc.A.canFocus=true;isc.A.shouldSaveValue=false;isc.A.height=null;isc.A.width=null;isc.A.titleStyle=null;isc.A.showTitle=false;isc.A.startRow=true;isc.A.endRow=true;isc.A.buttonConstructor=isc.Button;isc.A.autoFit=true;isc.A.buttonDefaults={click:function(){return this.canvasItem.handleClick()},doubleClick:function(){return this.canvasItem.handleDoubleClick()},getTitle:function(){return this.canvasItem.getTitle()}};isc.A.autoDestroy=true;isc.A=isc.ButtonItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$19d={width:true,height:true};isc.B.push(isc.A.getTitleHTML=function(){return this.getTitle()}
,isc.A.setTitle=function(_1){this.title=_1;if(this.canvas)this.canvas.setTitle(_1)}
,isc.A.$18y=function(){var _1={canFocus:this.$kk(),disabled:this.isDisabled(),width:this.width};if(this.height!=null)_1.height=this.height;if(this.icon)_1.icon=this.icon;if(this.titleStyle)_1.titleStyle=this.titleStyle;if(this.baseStyle)_1.baseStyle=this.baseStyle;if(this.autoFit!=null)_1.autoFit=this.autoFit;this.canvas=this.createAutoChild("button",_1,this.buttonConstructor);this.Super("$18y",arguments)}
,isc.A.$602=function(_1,_2,_3,_4){if(_1==null&&_2==null)return;return this.invokeSuper(isc.ButtonItem,"$602",_1,_2,_3,_4)}
,isc.A.propertyChanged=function(_1,_2){if(this.canvas!=null&&this.$19d[_1]){this.canvas.setProperty(_1,_2)}}
,isc.A.handleClick=function(){if(this.editingOn){if(isc.VisualBuilder&&isc.VisualBuilder.titleEditEvent=="click")this.editClick();return false}
return this.Super("handleClick",arguments)}
,isc.A.handleDoubleClick=function(){if(this.editingOn){if(isc.VisualBuilder&&isc.VisualBuilder.titleEditEvent=="doubleClick")this.editClick();return false}
return this.Super("handleDoubleClick",arguments)}
);isc.B._maxIndex=isc.C+7;if(isc.ListGrid){isc.ClassFactory.defineInterface("PickList");isc.ClassFactory.defineClass("PickListMenu","ScrollingMenu");isc.A=isc.PickListMenu;isc.A.$51t={};isc.A.pickListCacheLimit=50;isc.A=isc.PickListMenu.getPrototype();isc.A.useAllDataSourceFields=false;isc.A.tabIndex=-1;isc.A.canResizeFields=false;isc.A.canFreezeFields=false;isc.A.styleName="pickListMenu";isc.A.bodyStyleName="pickListMenuBody";isc.A=isc.PickListMenu.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$19e="background-color:";isc.A.$19f="color:";isc.B.push(isc.A.getValueIcon=function(_1,_2,_3){var _4=this.formItem;var _5=_4&&!_4.suppressValueIcons&&(_4.valueIcons!=null||_4.getValueIcon!=null);if(_5){var _6=_4.getValueFieldName(),_7=_4.valueIconField||_4.getDisplayFieldName()||_6;if(this.getFieldName(_1)==_7){return _4.$144(_3[_6])}}
return this.Super("getValueIcon",arguments)}
,isc.A.itemClick=function(_1){var _2=this.formItem,_3=_2.getValueFieldName(),_4=_1[_3];_2.pickValue(_4)}
,isc.A.hide=function(_1,_2,_3,_4){this.invokeSuper(isc.PickListMenu,"hide",_1,_2,_3,_4);if(!this.formItem)return;if(this.showModal)this.formItem.focusInItem();this.formItem.$19g=null;this.formItem.$19h();delete this.formItem.$19i}
,isc.A.show=function(){var _1=this.isVisible()&&this.isDrawn();this.bringToFront();this.Super("show",arguments);if(!_1)this.formItem.$19j()}
,isc.A.showClickMask=function(){if(!this.clickMaskUp(this.getID())){var _1=this.Super("showClickMask",arguments);if(this.formItem){var _2=this.formItem.form,_3=isc.EH.clickMaskRegistry.find("ID",_1);if(_3.$li)_3.$li=null}}}
,isc.A.getCellCSSText=function(_1,_2,_3){if(_1==this.selection.getSelectedRecord()){var _4=[];if(this.hiliteColor!=null)
_4[0]=this.$19e
_4[1]=this.hiliteColor
_4[2]=isc.$19k;if(this.hiliteTextColor!=null)
_4[3]=this.$19f;_4[4]=this.hiliteTextColor;_4[5]=isc.semi;return _4.join(isc.emptyString)}}
,isc.A.keyDown=function(){var _1=isc.EH.lastEvent.keyName;if(_1=="Tab"){this.hide();return false}}
,isc.A.$315=function(_1,_2,_3,_4,_5){var _6=this.getFieldName(_5);_1=this.formItem.formatPickListValue(_1,_6,_2);return this.Super("$315",[_1,_2,_3,_4,_5])}
,isc.A.bodyKeyPress=function(_1,_2){var _3=isc.EH.lastEvent.keyName;if(isc.Browser.isSafari){if(_3=="Tab"){this.hide();return false}}
var _4=isc.EH.getKeyEventCharacterValue();if(_4!=null){var _5=this.formItem.getAllLocalOptions();if(isc.isAn.Array(_5)&&_5.length>1){var _6=String.fromCharCode(_4),_6=_6.toLowerCase(),_7=this.formItem,_8=_7.getValueFieldName(),_9=_5.indexOf(this.getSelectedRecord()),_10=_9<(_5.length-1)?_9+1:0;if(_9<0)_9=0;while(_10!=_9){var _11=_5[_10][_8];_11=_7.mapValueToDisplay(_11);if(isc.isA.String(_11)&&_11.length>0&&_11.charAt(0).toLowerCase()==_6){this.scrollRecordIntoView(_10);this.$88(_10);return}
_10+=1;if(_10>=_5.length)_10=0}}}
if(this.getFocusRow()==null&&_3=="Enter"){this.cancel();return false}
return this.Super("bodyKeyPress",arguments)}
,isc.A.dataChanged=function(_1,_2,_3,_4){var _5=this.data;if(!_5)return;var _5=this.requestVisibleRows();if(_5&&Array.isLoading(_5[0])){return}
this.Super("dataChanged",arguments);var _6=this.formItem;if(_2&&this.getSelectedRecord()==_2&&_6){var _7=this.data.indexOf(_2),_8=_7==-1?null:this.data.get(_7);if(_8){var _9=_6.getValueFieldName();_6.setValue(_8[_9])}else{_6.clearValue()}}}
);isc.B._maxIndex=isc.C+10;isc.PickList.addInterfaceProperties({pickListHeight:300,emptyPickListHeight:100,pickListBaseStyle:"pickListCell",pickListAnimationTime:200,fetchDelay:200,pickListCellHeight:16,pickListHeaderHeight:22});isc.PickList.addInterfaceMethods({showPickList:function(_1,_2){this.$19g=true;if(!this.pickList)this.makePickList(_1);else this.setUpPickList(_1,_2);if(!_1&&(!this.pickList.isDrawn()||!this.pickList.isVisible())){this.$19l()}},$19l:function(){var _1=this.pickList;if(!this.isDrawn()||(this.shouldHideEmptyPickList()&&_1.getTotalRows()<1)){return}
this.placePickList();if(!_1.isDrawn()||!_1.isVisible()){if(this.animatePickList)this.pickList.animateShow("wipe",null,this.pickListAnimationTime);else
this.pickList.show()}},fetchData:function(_1,_2,_3){if(this.getOptionDataSource()==null){this.logWarn("fetchData() called for a non-databound pickList. Ignoring");return}
if(_2==null)_2={};if(_1!=null){if(_2.clientContext==null)_2.clientContext={};_2.clientContext.$03=_1}
_2.componentContext=this.form.ID+"."+this.name;if(!this.pickList)this.makePickList(false,_2);else this.filterDataBoundPickList(_2,!_3)},makePickList:function(_1,_2){var _3=isc.timeStamp();var _4=this.reusePickList();if(_4){this.pickList=this.getSharedPickList()}
if(!this.pickList){var _5=this.pickListProperties||{};if(this.sortField!=null)_5.sortField=this.sortField;if(this.sortFieldNum!=null)_5.sortFieldNum=this.sortFieldNum;if(this.sortDirection!=null)_5.sortDirection=this.sortDirection;this.pickList=isc.PickListMenu.create({headerHeight:this.pickListHeaderHeight},_5);var _6=this.pickList.dataProperties||{};if(this.filterLocally)_6.fetchMode="local";this.pickList.dataProperties=_6;if(_4)this.storeSharedPickList()}
this.setUpPickList(_1,false,_2);if(this.logIsInfoEnabled("timing"))
this.logInfo("Time to initially create pickList:"+(isc.timeStamp()-_3),"timing")},cachePickListResults:true,reusePickList:function(){return this.pickListProperties==null&&this.cachePickListResults},getSharedPickList:function(){if(this.$19m()){var _1=this.getOptionDataSource().getID(),_2=isc.PickListMenu.$51t[_1];if(_2){for(var i=0;i<_2.length;i++){if(_2[i].$51u==this.pickListFields){_2[i].$511=isc.timeStamp();return _2[i].$51v}}}
return null}else{return isc.PickList.$14w}},storeSharedPickList:function(){if(this.$19m()){var _1=this.getOptionDataSource().getID(),_2=isc.PickListMenu.$51t;if(!_2[_1])_2[_1]=[];var _3={$51v:this.pickList,$51u:this.pickListFields,$511:isc.timeStamp()}
_2[_1].add(_3);if(isc.PickListMenu.$512==null){isc.PickListMenu.$512=1}else{isc.PickListMenu.$512+=1;if(isc.PickListMenu.$512>isc.PickListMenu.pickListCacheLimit){var _4,_5=isc.timeStamp();for(var _1 in _2){var _6=_2[_1];for(var i=0;i<_6.length;i++){var _8=_6[i];if(_8.$511<=_5&&(_8!=_3)){_4=_8;_5=_8.$511}}}
if(_4){isc.PickListMenu.$512-=1;var _9=_4.$51v;var _6=_2[_9.getDataSource().getID()];_6.remove(_4);if(_9.$513!=null){for(var _10 in _9.$513){if(window[_10]&&window[_10].pickList==_9)
delete window[_10].pickList}}
_4.$51v.delayCall("destroy")}}}}else{isc.PickList.$14w=this.pickList}},getPickListCellHeight:function(){var _1=this.pickListCellHeight;if(this.valueicons!=null||this.getValueIcon!=null){var _2=this.getValueIconHeight();if(_2>_1)_1=_2}
return _1},setUpPickList:function(_1,_2,_3){var _4=this.pickList;var _5=this.pickListCellHeight;_4.setCellHeight(_5);this.$19n();this.setUpPickListFields();if(!_4.originalEmptyMessage)_4.originalEmptyMessage=_4.emptyMessage;_4.emptyMessage=this.emptyPickListMessage||_4.originalEmptyMessage;this.filterPickList(_1,_2,_3)},$19n:function(){var _1=this.pickList.formItem;if(_1==this)return;var _2=this.pickListProperties||{};isc.addProperties(_2,{formItem:this,baseStyle:this.pickListBaseStyle,hiliteColor:this.pickListHiliteColor,hiliteTextColor:this.pickListHiliteTextColor,showModal:this.modalPickList,dateFormatter:this.dateFormatter,dataArrived:function(_3,_4){this.Super("dataArrived",arguments);if(this.formItem)this.formItem.handleDataArrived(_3,_4,this)}});this.pickList.setProperties(_2);if(!this.pickList.$513)this.pickList.$513={};this.pickList.$513[this.getID()]=true;if(_1){if(this.pickList.isObserving(_1.containerWidget,"hide")){this.pickList.ignore(_1.containerWidget,"hide")}
if(this.pickList.isObserving(_1.containerWidget,"clear")){this.pickList.ignore(_1.containerWidget,"clear")}}
if(!this.pickList.isObserving(this.containerWidget,"hide")){this.pickList.observe(this.containerWidget,"hide","observer.hide();")}
if(!this.pickList.isObserving(this.containerWidget,"clear")){this.pickList.observe(this.containerWidget,"clear","if(observer.isDrawn())observer.clear();")}
this.pickList.markForRedraw()},getPickListFields:function(){if(this.pickListFields)return this.pickListFields;var _1=this.getDisplayFieldName(),_2;if(_1!=null){_2={width:"*",name:_1}
_2.formatCellValue=this.$500}else{_2={width:"*",name:this.getValueFieldName(),valueMap:this.getValueMap()}}
if(this.emptyDisplayValue!=null)_2.emptyCellValue=this.emptyDisplayValue;if(this.dateFormatter!=null){_2.type="date"}
_2.$720=true;return[_2]},$500:function(_1,_2,_3,_4,_5){if(_1!=null)return _1;var _6=_5.formItem,_7=_6.getValueFieldName()
if(_2[_7]==null)return _6.emptyCellValue;return _1},formatPickListValue:function(_1,_2,_3){if(this.pickList.getField(_2).$720){return this.$17c(_1)}
return _1},getPickListFilterCriteria:function(){var _1=this.optionCriteria||{};return isc.addProperties(_1,this.pickListCriteria)},getAllLocalOptions:function(){return this.$19m()?null:this.getClientPickListData()},$19o:function(_1,_2){var _3=this.getOptionDataSource();if(_3==null){return}
var _4=this.pickList&&!this.pickList.destroyed?this.pickList.data:null;if(!_4||!(_4.localData||_4.allRows))return;var _5=(_2?this.getValueFieldName():this.getDisplayFieldName()),_6=(_2?this.getDisplayFieldName():this.getValueFieldName());if(_5==_6)return _1;var _7=_4.allRows||_4.localData,_8=_7.find(_6,_1);if(_8!=null)return _8[_5]},$18q:"true",setUpPickListFields:function(){var _1=this.getPickListFields(),_2=this.pickList.fields;var _3=!_2||(_2.length!=_1.length);if(!_3){for(var i=0;i<_1.length;i++){var _5=_1[i],_6=_2[i];for(var _7 in _5){if(_5[_7]!=_6[_7]){_3=true;break}}
if(_3)break}}
if(!_3)return;for(var i=0;i<_1.length;i++){if(_1[i].showIf==null){_1[i].showIf=this.$18q}}
if(this.valueIcons!=null||this.getValueIcon!=null){for(var i=0;i<_1.length;i++){var _5=_1[i];if(_5[this.form.fieldIdProperty]==this.getValueFieldName()){if(_5.valueIconHeight==null)
_5.valueIconHeight=this.valueIconHeight;if(_5.valueIconWidth==null)
_5.valueIconWidth=this.valueIconWidth;if(_5.valueIconSize==null)
_5.valueIconSize=this.valueIconSize;if(_5.imageURLPrefix==null)
_5.imageURLPrefix=this.imageURLPrefix||this.baseURL||this.imgDir;if(_5.imageURLSuffix==null)
_5.imageURLSuffix=this.imageURLSuffix}}}
this.pickList.setFields(_1);if(_1.length>1){this.pickList.setHeaderHeight(this.pickListHeaderHeight);this.pickList.setShowHeader(true);this.pickList.fixedFieldWidths=true}else{this.pickList.setShowHeader(false);this.pickList.fixedFieldWidths=!this.autoSizePickList}},$19m:function(){if(this.optionDataSource)return true;if((this.showOptionsFromDataSource||!this.valueMap)&&this.getOptionDataSource()!=null)return true;return false},filterPickList:function(_1,_2,_3){if(!_2)
this.$19p(_1,_3);else{this.$43i=_1;this.fireOnPause("fetch",{target:this,methodName:"$19p",args:[null,_3,true]},this.fetchDelay)}},$19p:function(_1,_2,_3){if(_3)_1=this.$43i;delete this.$43i;this.$19i=_1;var _4=this.$19m();if(_4){var _5=this.getOptionDataSource();if(this.pickList.getDataSource()!=_5){this.pickList.setDataSource(_5,this.pickList.fields)}
this.filterDataBoundPickList(_2)}else{var _6=this.filterClientPickListData();if(this.pickList.data!=_6)this.pickList.setData(_6);this.filterComplete()}},$61x:function(_1){if(!this.$19m()||!_1)return;var _2=this.getOptionDataSource();if(_1.getDataSource()==_2&&_1.data){var _3=_1.data.context,_4=_1.data.criteria;if(_3.textMatchStyle!=this.textMatchStyle)return true;if(this.optionFilterContext!=null){for(var _5 in this.optionFilterContext){if(this.optionFilterContext[_5]!=_3[_5])return true}}
if(_2.compareCriteria(_4,this.getPickListFilterCriteria(),_3)==0){return false}}
return true},getFirstOptionValue:function(){var _1;if(this.$19m()){var _2=this.pickList||(this.reusePickList()?this.getSharedPickList():null);if(_2&&!this.$61x(_2)){var _3=_2.data.get(0);if(_3==null||Array.isLoading(_3)){_1=null}else{_1=_3[this.getValueFieldName()]}}else{this.fetchData(null,null,true)}}else{var _4=this.valueMap;if(isc.isAn.Array(_4))_1=_4[0];else if(isc.isAn.Object(_4)){for(var _5 in _4){_1=_5;break}}}
return _1},getClientPickListData:function(){return isc.PickList.optionsFromValueMap(this)},$19h:function(){if(this.pickListHidden)this.pickListHidden()},$19j:function(){if(this.pickListShown)this.pickListShown()},selectDefaultItem:function(){return this.selectItemFromValue(this.getValue())},selectItemFromValue:function(_1){var _2=this.pickList.getSelectedRecord(),_3=this.getValueFieldName();if(_2&&(_2[_3]==_1))return true;var _4=this.pickList.getData(),_5=this.$19m()?this.getOptionDataSource():null;if(_5!=null){var _6=_4.localData;if(_6)_2=_6.find(_3,_1)}else{_2=_4.find(_3,_1)}
if(_2){this.pickList.selection.selectSingle(_2);this.pickList.scrollRecordIntoView(_4.indexOf(_2))}
return(_2!=null)},filterComplete:function(_1,_2,_3){this.$43j=false;var _4=this.pickList;if(!_4||_4.destroyed)return;var _5=_4.hasFocus||(_4.body&&_4.body.hasFocus);var _2=_4.getData();if(_2.getLength()==0&&_4.isVisible()&&_4.isDrawn()){if(this.hideEmptyPickList){_4.hide();if(_5)this.focusInItem()}else{_4.setHeight(this.getPickListHeight());var _6=this.getPickListPosition();_4.placeNear(_6[0],_6[1])}}else{if(this.$19i)this.$19l();else if(_4.isVisible()&&_4.isDrawn())this.placePickList();delete this.$19i}
this.selectDefaultItem();if(this.$43k){delete this.$43k;this.$43f(this._value)}
this.$19v();var _7=(_3&&_3.clientContext?_3.clientContext.$03:null);if(_7){this.fireCallback(_7,"item,dsResponse,data,dsRequest",[this,_1,_2,_3])}},$19v:function(){if(this.isDrawn()&&this.getValueFieldName()!=null&&this.$19m())
{var _1;if(!this.$10v())_1=this.getValue();else{if(this.isA("SelectItem"))_1=this.$19w;else _1=this.mapDisplayToValue(this.getElementValue())}
var _2=this.getSelectedRecord();if(_2){var _3=this.mapValueToDisplay(_1);if(this.$19z!=_3)this.setElementValue(_3)}}},filterDataBoundPickList:function(_1,_2){if(isc.$cv)arguments.$cw=this;var _3=this.getPickListFilterCriteria(),_4={textMatchStyle:this.textMatchStyle,showPrompt:false};if(this.optionFilterContext!=null)isc.addProperties(_4,this.optionFilterContext);if(this.optionOperationId!=null)_4.operationId=this.optionOperationId;if(_1!=null){isc.addProperties(_4,_1)}
var _5=false;if(this.pickList.data&&isc.ResultSet&&isc.isA.ResultSet(this.pickList.data)){if(_2){this.pickList.data.$394()}else if(!this.pickList.data.willFetchData(_3,this.textMatchStyle)){_5=true}}
this.pickList.filterData(_3,{target:this,methodName:"filterComplete"},_4);if(_5)this.filterComplete();else this.$43j=true},handleDataArrived:function(_1,_2,_3){if(this.defaultToFirstOption&&this.getValue()==null&&_1==0){this.setToDefaultValue()}
if(this.dataArrived)this.dataArrived(_1,_2,_3)},dataArrived:function(_1,_2,_3){},textMatchStyle:"startsWith",$19q:"substring",separatorRows:[{isSeparator:true}],filterClientPickListData:function(){var _1=this.getClientPickListData(),_2=this.getPickListFilterCriteria();if(_2==null||isc.isA.emptyObject(_2))return _1;var _3=[],_4;if(this.showAllOptions)_4=this.separatorRows.duplicate();var _5=false;for(var _6 in _2){var _7=_2[_6];if(!_7||isc.isA.emptyString(_7))continue;_5=true;if(!isc.isA.String(_7))_7+=isc.emptyString;_7=_7.toLowerCase();var _8=_1.getLength(),_9=this.getValueFieldName();for(var i=0;i<_8;i++){var _11=_1[i][_6];if(this.filterDisplayValue&&_6==_9){_11=this.mapValueToDisplay(_11)}
if(!isc.isA.String(_11))_11+="";_11=_11.toLowerCase();if((this.textMatchStyle==this.$19q&&!_11.contains(_7))||(this.textMatchStyle!=this.$19q&&!isc.startsWith(_11,_7)))
{if(this.showAllOptions)_4.add(_1[i])}else{_3.add(_1[i])}}}
if(!_5)_3=_1.duplicate();if(this.showAllOptions&&_4.length>1)_3.addList(_4);return _3},shouldHideEmptyPickList:function(){if(this.hideEmptyPickList!=null)return this.hideEmptyPickList;return!this.$19m()},getPickListPosition:function(){return[this.getPageLeft(),this.getPageTop()+this.getHeight()]},getPickListHeight:function(){var _1=this.pickListHeight,_2=this.pickList,_3=_2.getTotalRows();if(_3==0)return this.emptyPickListHeight;var _4=_2.cellHeight,_5=(_2.showHeader?_2.headerHeight:0),_6=_3*_4,_7=(_2.body?_2.body.getVBorderPad():isc.Element.$yo(_2.bodyStyleName)),_8=_6+_5+_2.getVBorderPad()+_7;return Math.min(_8,_1)},$19r:function(_1,_2,_3){var _4=this.pickList,_5=_1;if(_4.fixedFieldWidths){_4.resizeTo(_5,_2);return}
var _6=_4.body||_4,_7=(_3?_6.getScrollbarSize():0);if(_4.body){_4.body.setOverflow(isc.Canvas.HIDDEN)}else{_4.bodyOverflow=isc.Canvas.HIDDEN}
var _8=_4.leaveScrollbarGap;_4.setProperties({leaveScrollbarGap:false});_4.resizeTo(_1-_7,_2);if(!_4.isDrawn()){_4.visibility=isc.Canvas.HIDDEN;_4.draw()}else if(_4.body.isDirty()){_4.body.redraw("pickList sizing")}
var _9=_4.body.getScrollWidth(),_10=(_4.body?_4.body.getHBorderPad():isc.Element.$yp(_4.bodyStyleName)),_11=_9+_7+_4.getHBorderPad()+_10;_4.setProperties({leaveScrollbarGap:_8});if(this.logIsDebugEnabled("sizing")){this.logDebug("pickList auto-sizing: minWidth : "+_1+(_3?", vscrollOn":"")+", width for columns: "+(_1-_7)+", scrollWidth: "+_9+", drawnWidth: "+_11+", borderPad: "+_4.getHBorderPad(),"sizing")}
_4.setWidth(Math.max(_11,_1));_4.body.setOverflow(isc.Canvas.AUTO)},placePickList:function(){var _1=this.pickList,_2=this.getPickListPosition(),_3=_2[0],_4=_2[1],_5=this.getPickListHeight(),_6=(_5==this.pickListHeight);if(!_6){_1.setProperties({leaveScrollbarGap:false})}else{_1.setProperties({leaveScrollbarGap:true})}
this.$19r(this.pickListWidth!=null?this.pickListWidth:this.getElementWidth(),_5,(_5==this.pickListHeight));_1.placeNear(_3,_4)},getSelectedRecord:function(){if(this.pickList==null||this.pickList.destroyed)this.makePickList(false);if(this.selectItemFromValue(this.getValue()))return this.pickList.getSelectedRecord();return null},pickValue:function(_1){}});isc.A=isc.PickList;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.optionsFromValueMap=function(_1){var _2=_1.getValueMap(),_3=[];if(_2==null)_2=[];var _4=_1.getValueFieldName();if(isc.isAn.Array(_2)){for(var i=0;i<_2.length;i++){_3[i]={}
_3[i][_4]=_2[i]}}else if(isc.isAn.Object(_2)){var i=0;for(var j in _2){_3[i]={};_3[i][_4]=j;i++}}
return _3}
);isc.B._maxIndex=isc.C+1}
if(isc.ListGrid){isc.ClassFactory.defineClass("SelectItem","FormItem");isc.$19s={textBoxStyle:"selectItemText",height:19,width:150,dirtyOnKeyDown:false,changeOnKeypress:false,redrawOnShowIcon:false,addUnknownValues:true,autoSizePickList:true,separatorTitle:"--------------------",separatorValue:"----",otherTitle:"Other...",otherValue:"***other***"};isc.SelectItem.addProperties(isc.$19s)
isc.A=isc.SelectItem.getPrototype();isc.A.showPickerIcon=true;isc.A.emptyDisplayValue="&nbsp;";isc.A.controlStyle="selectItemControl";isc.A.pickerIconStyle="selectItemPickerIcon";isc.A.canFocus=true;isc.A.showFocused=true;isc.A.pickerIconSrc="[SKIN]/DynamicForm/SelectItem_PickButton_icon.gif";isc.A.pickerIconDefaults={tabIndex:-1,click:function(){}};isc.A.clipValue=true;isc.A.showOver=true;isc.A.modalPickList=true;isc.A.changeOnValueChange=true;isc.A.changeOnKeyboardNavigation=true;isc.A.canSelectText=false;isc.A.allowEmptyValue=false;isc.A.autoFetchData=true;isc.A=isc.SelectItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.selectOtherPrompt="Other value for <br>${item.getTitle()}?";isc.A.dialogWidth=250;isc.B.push(isc.A.init=function(){if(this.hiliteOnFocus!=null){this.$15o("hiliteOnFocus","showFocused");this.showFocused=this.hiliteOnFocus}
if(this.pickButtonWidth!=null){this.$15o("pickButtonWidth","pickerIconWidth");this.pickerIconWidth=this.pickButtonWidth}
if(this.pickButtonHeight!=null){this.$15o("pickButtonHeight","pickerIconHeight");this.pickerIconHeight=this.pickButtonHeight}
if(this.pickButtonSrc!=null){this.$15o("pickButtonSrc","pickerIconSrc");this.pickerIconSrc=this.pickButtonSrc}
return this.Super("init",arguments)}
,isc.A.drawn=function(_1,_2,_3,_4){this.invokeSuper(isc.SelectItem,"drawn",_1,_2,_3,_4);if(this.autoFetchData&&this.$19m()){this.fetchData(null,null,true)}}
,isc.A.$16m=function(){return 0}
,isc.A.$121=function(_1,_2){var _3=this.$117(_1);if(_3==this.getPickerIcon()){_2.blur();this.focusInItem();return}
return this.Super("$121",arguments)}
,isc.A.handleMouseMove=function(){if(this.showOver&&!this.isDisabled()){if(this.$17a())this.$164(this.getPickerIcon(),true);else this.$164(this.getPickerIcon(),false)}
return this.Super("handleMouseMove",arguments)}
,isc.A.handleMouseOut=function(){if(this.showOver&&!this.isDisabled()){this.$164(this.getPickerIcon(),false)}
return this.Super("handleMouseOut",arguments)}
,isc.A.$114=function(_1,_2,_3,_4,_5){if(this.$117(_1)==this.getPickerIcon()&&this.$17a())return;return this.invokeSuper("SelectItem","$114",_1,_2,_3,_4,_5)}
,isc.A.handleClick=function(){if(!this.isDisabled()){this.focusInItem();this.showPickList()}
return this.Super("handleClick",arguments)}
,isc.A.handleKeyPress=function(_1,_2){var _3=this.Super("handleKeyPress",arguments);if(_3==false)return false;var _4=_1.keyName;if(_4=="Enter"&&this.isSelectOther){if(this.$19u!=null)this.updateValue()}else if(_4=="Arrow_Down"){if(isc.EH.altKeyDown())this.showPickList();else this.moveToNextValue(1);_3=false}else if(_4=="Arrow_Up"){if(isc.EH.altKeyDown())this.showPickList();else this.moveToNextValue(-1);_3=false}else if(_4=="Home"){this.moveToFirstValue();_3=false}else if(_4=="End"){this.moveToLastValue();_3=false}else{var _5=_1.characterValue;if(_5!=null){this.moveToChar(_5)}}
return _3}
,isc.A.$12x=function(_1,_2){var _3=this.Super("$12x",arguments);if(this.changeOnBlur||this.$10v()||this.$19u){if(isc.Browser.isMoz&&this.$19u==this.otherValue)
this.form.$11w=true;this.updateValue()}
return _3}
,isc.A.handleEditorExit=function(){if(this.$19g)return;return this.Super("handleEditorExit",arguments)}
,isc.A.editorEnter=function(_1,_2,_3){this.$66s()}
,isc.A.editorExit=function(_1,_2,_3){var _4;if(this.showHintInField&&(_3===_4||_3==null||isc.is.emptyString(_3)))
{this.$66t()}}
,isc.A.$19j=function(){this.handleEditorEnter();if(this.pickListShown)this.pickListShown()}
,isc.A.getAllLocalOptions=function(){var _1;if(this.$19m()){if(!this.pickList||this.pickList.destroyed)return;var _2=this.pickList.data;if(!_2||!_2.lengthIsKnown()||!_2.allMatchingRowsCached())return;var _3=this.getPickListFilterCriteria();if(_2.compareCriteria(_3,_2.criteria)!=0){if(!_2.allRowsCached()||!_2.useClientFiltering)return;this.filterPickList(false,false)}
_1=_2.getAllRows()}else{_1=this.getClientPickListData()}
return _1}
,isc.A.moveToChar=function(_1){var _2=this.getAllLocalOptions();if(!_2||_2.length<2)return;var _3=String.fromCharCode(_1);if(_3==null)return;_3=_3.toLowerCase();var _4=(this.isSelectOther&&this.$19u!=null)?this.$19u:(this.$10v()?this.$19w:this.getValue()),_5=this.getValueFieldName(),_6=_2.findIndex(_5,_4),i=(_6==_2.length-1?0:_6+1);while(i!=_6){if(_6<0)_6=0;var _8=_2[i][this.getValueFieldName()],_9=this.mapValueToDisplay(_8);if(isc.isA.String(_9)){var _10=_9.charAt(0).toLowerCase();if(_10==_3){var _11=_8;this.changeToValue(_11,(this.changeOnValueChange&&this.changeOnKeyboardNavigation));return}}
i+=1;if(i>=_2.length)i=0}}
,isc.A.moveToNextValue=function(_1){var _2=this.getAllLocalOptions();if(!_2||_2.length<2)return;var _3;if(this.isSelectOther&&this.$19u!=null)_3=this.$19u;else _3=(this.$10v()?this.$19w:this.getValue());var _4=this.getValueFieldName(),_5=_2.findIndex(_4,_3);_5+=_1;if(_5>=_2.length||_5<0)return;var _6=_2[_5][_4];this.changeToValue(_6,(this.changeOnValueChange&&this.changeOnKeyboardNavigation))}
,isc.A.moveToFirstValue=function(){if(this.optionDataSource)return;var _1=this.getClientPickListData(),_2=this.getValueFieldName(),_3=_1[0][_2];this.changeToValue(_3,(this.changeOnValueChange&&this.changeOnKeyboardNavigation))}
,isc.A.moveToLastValue=function(){if(this.optionDataSource)return;var _1=this.getClientPickListData(),_2=this.getValueFieldName(),_3=_1[_1.length-1][_2]
this.changeToValue(_3,(this.changeOnValueChange&&this.changeOnKeyboardNavigation))}
,isc.A.$kk=function(){return true}
,isc.A.$19x=function(){if(!this.$19y){this.$19y=new Function("if(window."+this.getID()+")window."+this.getID()+".$19g=true;")}
return this.$19y}
,isc.A.$159=function(_1,_2,_3,_4){this.invokeSuper(isc.SelectItem,"$159",_1,_2,_3,_4);if(isc.Browser.isIE){var _5=this.$16v(this.getPickerIcon());if(_5){_5.onmousedown=this.$19x()}}}
,isc.A.makePickList=function(_1){if(!this.filterLocally&&this.allowEmptyValue&&this.$19m()){if(this.pickListProperties==null)
this.pickListProperties={};if(this.pickListProperties.dataProperties==null)
this.pickListProperties.dataProperties={};this.pickListProperties.dataProperties.fetchMode="basic"}
var _2=isc.PickList.getPrototype().makePickList;return _2.apply(this,arguments)}
,isc.A.changeToValue=function(_1,_2){var _3=(this.$19u||this.$19w||this.getValue());if(_3==_1)return;if(this.isSelectOther&&(_1==this.separatorValue||_1==this.otherValue))
{this.setElementValue(this.mapValueToDisplay(_1));this.$19u=_1;return}else{delete this.$19u}
this.setLocalValue(_1);if(_2)this.updateValue()}
,isc.A.setLocalValue=function(_1){this.$19w=_1;if(this.isVisible()&&this.containerWidget.isDrawn()){if(_1==null)_1=null;this.setElementValue(this.mapValueToDisplay(_1),_1)}
this.$18i()}
,isc.A.setElementValue=function(_1,_2,_3,_4,_5){this.$19z=_1;if(this.showHintInField&&this.getHint()){var _6;if(_1===_6||_1==null||isc.is.emptyString(_1))
{if(this.hasDataElement()){var _7=this.getDataElement();_7.className=this.$66v()}else{var _8=this.$15h();if(_8!=null)
_8.className=this.$66v()}
var _9=this.getHint();if(_9)_9=_9.unescapeHTML();_1=_9;this.$66u=true}}
return this.invokeSuper(isc.SelectItem,"setElementValue",_1,_2,_3,_4,_5)}
,isc.A.updateValue=function(){if(this.isSelectOther&&this.$19u!=null){var _1=this.getSelectOtherValue(this.$19u);delete this.$19u;this.setLocalValue(_1)}
if(!this.$10v())return;var _2=this.$19w;this.$10y(_2)}
,isc.A.mapDisplayToValue=function(_1){return _1}
,isc.A.getSelectOtherValue=function(_1){if(_1==this.separatorValue)return(this.$19w||this.getValue());if(_1==this.otherValue){var _2=this.$19w||this.getValue(),_3=(_2==null?"":this.mapValueToDisplay(_2)),_4=this.selectOtherPrompt.evalDynamicString(null,{item:this,value:_2}),_5=isc.addProperties({width:this.dialogWidth},this.dialogDefaults,this.dialogProperties);isc.askForValue(_4,this.getID()+".getSelectOtherValueCallback(value)",_5);return true}}
,isc.A.getSelectOtherValueCallback=function(_1){if(_1!=null){_1=this.mapDisplayToValue(_1);this.changeToValue(_1,this.changeOnValueChange)}}
,isc.A.setValue=function(_1,_2,_3,_4){_1=this.$190(_1);var _5,_6=this.$19w;if(_6===_5)_6=this._value;this.invokeSuper(isc.SelectItem,"setValue",_1,_2,_3,_4);_1=this.getValue();if(_1!=_6)this.setLocalValue(_1);if(this.pickList&&this.pickList.isDrawn()&&this.pickListVisible()){this.setUpPickList(true)}
if(!this.hasFocus&&this.showHint&&this.showHintInField&&this.getHint()){if(_1===_5||_1==null||isc.is.emptyString(_1)){this.$66t()}}
return _1}
,isc.A.saveValue=function(_1,_2,_3,_4,_5){var _6=this._value;if(this.$191(_6,_1))delete this.$192;return this.invokeSuper(isc.SelectItem,"saveValue",_1,_2,_3,_4,_5)}
,isc.A.$191=function(_1,_2){return(this.addUnknownValues&&this.$192&&((_1!=null&&!this.$193(_1))||(_2!=null&&!this.$193(_2))))}
,isc.A.$10x=function(_1,_2,_3,_4){this.invokeSuper(isc.SelectItem,"$10x",_1,_2,_3,_4);delete this.$19w}
,isc.A.getDefaultValue=function(){var _1=this.Super("getDefaultValue",arguments);if(_1==null&&this.defaultToFirstOption)_1=this.getFirstOptionValue();return this.$190(_1)}
,isc.A.$190=function(_1){if(!this.$194(_1)){var _2=this.$195;this.$195=true;var _3;if(_2)_3=_1;else _3=this.$19w||this.getValue();if(_1==_3||!this.$194(_3)){_3=null}
_1=_3}
return _1}
,isc.A.$194=function(_1){if(this.addUnknownValues||this.optionDataSource)return true;if(_1==null)return true;return this.$193(_1)}
,isc.A.$193=function(_1){var _2=this.getValueMap(),_3;if(isc.isAn.Array(_2)){return _2.contains(_1)}else if(isc.isAn.Object(_2)){return(_2[_1]!==_3)}
return false}
,isc.A.mapValueToDisplay=function(_1,_2,_3,_4){if(this.isSelectOther){if(_1==this.otherValue)return this.otherTitle;if(_1==this.separatorValue)return this.separatorTitle}
return this.invokeSuper(isc.SelectItem,"mapValueToDisplay",_1,_2,_3,_4)}
,isc.A.getSelectedRecord=function(){if(this.pickList==null||this.pickList.destroyed)this.makePickList(false);var _1,_2=this.$19w;if(_2===_1)_2=this.getValue();if(this.selectItemFromValue(_2)){return this.pickList.getSelectedRecord()}
return null}
,isc.A.$17b=function(_1,_2,_3,_4,_5,_6){var _7=this.invokeSuper(isc.SelectItem,"$17b",_1,true,_3,_4,_5,_6);if(_7==null&&this.getDisplayFieldName()!=null)
_7=this.$19o(_1,false);if(_7==null&&!_2)_7=_1;return _7}
,isc.A.$43f=function(_1,_2){var _3=(this.$17b(_1,true)!=null);if(_3)return;if(this.$43j){this.$43k=true;return}
this.invokeSuper(isc.ComboBoxItem,"$43f",_1)}
,isc.A.getClientPickListData=function(){if(this.$192)return this.$192;var _1=isc.PickList.optionsFromValueMap(this),_2=this.getValueFieldName();if(this.allowEmptyValue&&(_1.find(_2,null)==null)){var _3={};_1.addAt(_3,0)}
var _4=this.getValue();if(_4!=null&&_1.find(_2,_4)==null){var _5={};_5[_2]=_4;_1.addAt(_5,0)}
if(this.isSelectOther){var _6={},_7={};_6[_2]=this.separatorValue;_7[_2]=this.otherValue;_1.addListAt([_6,_7],_1.length)}
this.$192=_1;return _1}
,isc.A.formatPickListValue=function(_1,_2,_3){if(this.isSelectOther&&(_2==this.getValueFieldName())){if(_1==this.otherValue)return this.otherTitle;if(_1==this.separatorValue)return this.separatorTitle}
if(this.pickList.getField(_2).$720){return this.$17c(_1)}
return _1}
,isc.A.pickValue=function(_1){if(this.isSelectOther){if(this.getSelectOtherValue(_1))return}
this.changeToValue(_1,this.changeOnValueChange)}
,isc.A.getPickListPosition=function(){var _1=this.getPageTop(),_2=_1+this.getHeight(),_3=this.getPageLeft(),_4=this.getPickListHeight(),_5=isc.Page.getScrollTop(),_6=isc.Page.getHeight()+_5;if(_2+_4>_6){_2=Math.max(_5,(_1-_4))}
return[_3,_2]}
,isc.A.setValueMap=function(){this.Super("setValueMap",arguments);if(this.$192)delete this.$192;if(this.hasPickList()){if(this.pickList.isVisible())this.pickList.hide();delete this.pickList.formItem}
var _1=this.getValue(),_2=this.$190(_1);if(_1!=_2){this.setValue(_2)}else{this.setElementValue(this.mapValueToDisplay(_2))}}
,isc.A.hasPickList=function(){return(this.pickList&&!this.pickList.destroyed&&this.pickList.formItem==this)}
,isc.A.pickListVisible=function(){return(this.hasPickList()&&this.pickList.isDrawn()&&this.pickList.isVisible())}
,isc.A.cleared=function(){if(this.pickListVisible())this.pickList.hide();return this.Super("cleared",arguments)}
,isc.A.filterComplete=function(){if(this.allowEmptyValue&&this.$19m()){var _1=this.pickList.data,_2=_1.isLocal()?_1.allRows:_1.localData;if(_2&&!_2.find(this.getValueFieldName(),null)){_1.insertCacheData({},0)}}
var _3=isc.PickList.getPrototype().filterComplete;_3.apply(this,arguments)}
);isc.B._maxIndex=isc.C+51;isc.ClassFactory.mixInInterface("SelectItem","PickList");isc.SelectItem.registerStringMethods({dataArrived:"startRow,endRow,data",getPickListFilterCriteria:""})}
isc.ClassFactory.defineClass("NativeSelectItem","FormItem");isc.A=isc.NativeSelectItem;isc.A.DEFAULT_ROW_COUNT=6;isc.A.instances=[];isc.NativeSelectItem.addProperties(isc.$19s)
isc.A=isc.NativeSelectItem.getPrototype();isc.A.height=null;isc.A.$125=true;isc.A.$15i=true;isc.A.$16b={onchange:isc.FormItem.$12y};isc.A=isc.NativeSelectItem;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.getOptionsHTML=function(_1,_2){var _3=isc.SB.create();if(isc.isAn.Array(_1)){for(var i=0,_5=_1.length;i<_5;i++){var _6=_1[i];_3.append(this.$196(_6,_6,_2))}}else{for(var _6 in _1){var _7=_1[_6];_3.append(this.$196(_6,_7,_2))}}
return _3.toString()}
,isc.A.$196=function(_1,_2,_3){var _4=this.$197;if(!_4){this.$198=" SELECTED ";_4=this.$197=[];_4[0]="<OPTION ";_4[2]=' VALUE="';_4[4]='">';_4[6]="</OPTION>"}
_4[1]=(_1==_3?this.$198:null);_4[3]=_1;_4[5]=_2;return _4.join(isc.$ad)}
,isc.A.getOptionCount=function(_1){if(isc.isAn.Array(_1)){return _1.length}else{var _2=0;for(var _3 in _1){_2++}
return _2}}
);isc.B._maxIndex=isc.C+3;isc.A=isc.NativeSelectItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.Super("init",arguments);isc.NativeSelectItem.instances.add(this);if(this.optionDataSource){var _1=this.getOptionDataSource();var _2=this.getValueFieldName();var _3=this.getDisplayFieldName();var _4=this;_1.fetchData(null,function(_8,_9){var _5;if(!_3)_5=[];else _5={};for(var i=0;i<_9.getLength();i++){var _7=_9[i];if(!_3){_5.add(_7[_2])}else{_5[_7[_2]]=_7[_3]}}
_4.setValueMap(_5)})}}
,isc.A.destroy=function(){isc.NativeSelectItem.instances.remove(this);this.Super("destroy",arguments)}
,isc.A.getTextBoxCellCSS=function(){return this.$136}
,isc.A.getElementHTML=function(_1,_2){this.$199=false;var _3=this.form,_4=_3.getID(),_5=isc.StringBuffer.newInstance(),_6=this.getItemID();var _7=isc.$ad;var _8=this.$xq(_2);if(_8!=null)_5.append(_8);if(!this.showValueIconOnly){_5.append("<SELECT"," NAME=",this.getElementName()," ID=",this.getDataElementId(),this.$155(),(!this.showTitle&&this.accessKey!=null?" ACCESSKEY="+this.accessKey:_7),(this.isDisabled()?" DISABLED ":_7),this.getElementStyleHTML(),(this.multiple?" MULTIPLE":_7)," TABINDEX=",this.$154()," handleNativeEvents=false>");_5.append(this.getOptionsHTML(this.getValueMap()));_5.append("</SELECT>")}
return _5.toString()}
,isc.A.$12z=function(_1){if(isc.Browser.isIE&&!_1){isc.Timer.setTimeout(this.getID()+".$12z(true)",10);return true}
return this.form.elementChanged(this.getID())}
,isc.A.$12x=function(_1,_2){var _3=this.Super("$12x",arguments);if(this.changeOnBlur)this.form.elementChanged(this)}
,isc.A.getOptionsHTML=function(_1){var _2=isc.NativeSelectItem.getOptionsHTML(_1?_1:this.getValueMap());if(this.isSelectOther){_2+="<OPTION VALUE=\""+this.separatorValue+"\">"+this.separatorTitle+"<OPTION VALUE=\""+this.otherValue+"\">"+this.otherTitle}
return _2}
,isc.A.getOptionCount=function(_1){return isc.NativeSelectItem.getOptionCount(_1?_1:this.getValueMap())}
,isc.A.getElementStyleHTML=function(){var _1=isc.SB.create(),_2=isc.SB.create();if(this.textBoxStyle!=null)_1.append(" CLASS='",this.getTextBoxStyle(),"' ");if(this.multiple||this.rows){var _3=this.rows;if(!isc.isA.Number(_3)||_3<1)
_3=Math.min(isc.NativeSelectItem.DEFAULT_ROW_COUNT,this.getOptionCount());if(this.height){if(isc.isA.Number(this.height))_2.append("HEIGHT:",this.height,"px;")}
_1.append(" SIZE=",_3)}
if(isc.Browser.isDOM){var _4=this.getElementWidth();if(isc.isA.Number(_4)){_4=Math.max(_4,1);_2.append("WIDTH:",_4,"px;")}
if(isc.Browser.isMoz){_2.append("-moz-user-focus:",(this.$154()>0?"normal;":"ignore;"))}
_2.append("margin-top:0px;margin-bottom:0px;");_2=_2.toString();if(_2.length>0)_1.append(" STYLE='",_2,"'")}
return _1.toString()}
,isc.A.$16z=function(){if(!isc.isA.Number(this.width))return;return this.Super("$16z",arguments)}
,isc.A.$16m=function(){return 0}
,isc.A.mapValueToDisplay=function(_1){if(isc.isAn.Array(_1)){var _2=[];for(var i=0;i<_1.length;i++){_2[i]=this.mapValueToDisplay(_1[i])}
return _2}
return this.Super("mapValueToDisplay",arguments)}
,isc.A.setElementValue=function(_1,_2){if(arguments.length==1)_2=_1;var _3=this.getDataElement();if(!_3)return null;var _4=_3.options;if(!_4){this.logDebug("setElementValue(): element.options is null. ???");return null}
this.$17j(_2);if(!this.multiple){if(_2==null)_2="";for(var i=0;i<_4.length;i++){if(_4[i].value==_2){if(_3.selectedIndex!=i){_3.selectedIndex=i}
return _3.selectedIndex}}
for(var i=0;i<_4.length;i++){if(_4[i].text==_2){if(_3.selectedIndex!=i){_3.selectedIndex=i}
_3.selectedIndex=i;return _3.selectedIndex}}
if(this.addUnknownValues){if(isc.Browser.isIE){var _6=0;if(this.$199){_4[_6].text=_1;_4[_6].value=_2}else{_4.add(new Option(_2,_1),_6);this.$199=true}}else{if(this.$199){var _6=_4.length-1;_4[_6].value=_2;_4[_6].text=_1}else{var _6=_4.length;_4[_6]=new Option(_2,_1);this.$199=true}}
if(_3.selectedIndex!=_6){_3.selectedIndex=_6}
return _3.selectedIndex}else{return null}}else{if(_2==null){_2=[]}else if(isc.isA.String(_2)&&_2.contains(",")){_2=_2.split(",")}else if(!isc.isAn.Array(_2)){_2=[_2]}else{_2=_2.duplicate()}
if(_1==null){_1=[]}else if(isc.isA.String(_1)&&_1.contains(",")){_1=_1.split(",")}else if(!isc.isAn.Array(_1)){_1=[_1]}else{_1=_1.duplicate()}
for(var i=0;i<_4.length;i++){var _7=_3.options[i];var _8=_2.indexOf(_7.value);if(_8>-1){if(_7.selected!=true)_7.selected=true;_2.removeItem(_8)}else{if(_7.selected!=false)_7.selected=false}}
if(_2.length!=0){for(var i=0;i<_4.length;i++){var _7=_3.options[i];var _8=_2.indexOf(_7.text);if(_8>-1){if(_7.selected!=true)_7.selected=true;_2.removeItem(_8)}}}
if(_2.length!=0&&this.addUnknownValues){for(var i=0;i<_2.length;i++){var _9=_4[_4.length]=new Option(_2[i],_1[i]);_9.selected=true}}
return _2}}
,isc.A.getElementValue=function(){var _1=this.getDataElement();if(!_1)return null;var _2=_1.options;if(!_2||_2.length==0)return null;if(!this.multiple){var _3=_2[_1.selectedIndex];if(!_3)return null;return(_3.value!=null?_3.value:_3.text)}else{var _4=[];for(var i=0;i<_2.length;i++){var _3=_2[i];if(_3.selected){_4.add(_3.value!=null?_3.value:_3.text)}}
if(_4.length<2)return _4[0];return _4}}
,isc.A.setElementValueMap=function(_1){this.$199=false;this.Super("setElementValueMap",arguments);var _2=this.getDataElement();if(_2==null)return;var _3=_2.options;_3.length=0;if(isc.isAn.Array(_1)){for(var i=0;i<_1.length;i++){_3[i]=new Option(_1[i],_1[i])}}else{for(var _5 in _1){_3[_3.length]=new Option(_1[_5],_5)}}
if(this.isSelectOther){_3[_3.length]=new Option(this.separatorTitle,this.separatorValue);_3[_3.length]=new Option(this.otherTitle,this.otherValue)}}
,isc.A.updateValue=function(){if(this.isSelectOther){if(!this.hasElement()||this.getDataElement()==null)return;var _1=this._value,_2=this.getElementValue();if(_2==this.separatorValue){this.setValue(_1);return false}
if(_2==this.otherValue){var _3=this.getValueMapTitle(_1);_2=prompt("Other value for \r'"+this.getTitle()+"'?",(_3?_3:""));if(_2==null){this.setValue(_1);return false}
this.setElementValue(_2)}}
return this.Super("updateValue",arguments)}
);isc.B._maxIndex=isc.C+16;isc.defineClass("CycleItem","FormItem");isc.A=isc.CycleItem.getPrototype();isc.A.canSelectText=false;isc.A.canFocus=true;isc.A.iconVAlign="middle";isc.A=isc.CycleItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.handleKeyPress=function(){var _1=isc.EH.getKey();if(_1=="Space"){this.advanceValue();return false}
return this.Super("handleKeyPress",arguments)}
,isc.A.handleClick=function(){if(this.isDisabled())return;if(!this.hasFocus)this.focusInItem();this.advanceValue()}
,isc.A.handleDoubleClick=function(){if(this.isDisabled())return;this.advanceValue()}
,isc.A.advanceValue=function(){var _1=this.getValueMap();if(isc.isA.Object(_1)&&!isc.isA.Array(_1)){_1=isc.getKeys(_1)}
if(_1==null||_1.length<2){this.logInfo("CycleItem is non interactive as there are no options for this item.");return}
var _2=this.getValue(),_3=_1.indexOf(_2);if(_3==_1.length-1)_3=-1;var _4=_1[_3+1];if(!this.compareValues(_4,this._value)){var _5=this.mapValueToDisplay(_4);this.setElementValue(_5,_4);this.$10y(_4)}}
);isc.B._maxIndex=isc.C+4;isc.defineClass("CheckboxItem","CycleItem");isc.A=isc.CheckboxItem;isc.A.trueFalseValueMap=[true,false];isc.A.trueFalseNullValueMap=[true,false,null];isc.A=isc.CheckboxItem.getPrototype();isc.A.textBoxStyle="labelAnchor";isc.A.showLabel=true;isc.A.height=20;isc.A.requiredTitlePrefix="<b>";isc.A.requiredTitleSuffix="</b>";isc.A.valueIconLeftPadding=4;isc.A.valueIconRightPadding=3;isc.A.showValueIconOver=true;isc.A.showValueIconFocused=true;isc.A.showValueIconDown=true;isc.A.showValueIconDisabled=true;isc.A.checkedImage="[SKINIMG]/DynamicForm/checked.gif";isc.A.uncheckedImage="[SKINIMG]/DynamicForm/unchecked.gif";isc.A.partialSelectedImage="[SKINIMG]/DynamicForm/partialcheck.gif";isc.A.unsetImage="[SKINIMG]/DynamicForm/unsetcheck.gif";isc.A.valueIconWidth=13;isc.A.valueIconHeight=13;isc.A=isc.CheckboxItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.allowEmptyValue=false;isc.A.$18q="true";isc.A.$18r="false";isc.A.emptyValueKey="**NULL**";isc.A.$13u="Over";isc.A.$13v="Down";isc.A.$13w="Disabled";isc.A.$20a="height:";isc.A.$ph="px";isc.B.push(isc.A.getValueMap=function(){if(this.$20b)return this.$20b;var _1=this.Super("getValueMap",arguments);if(_1!=null){var _2=false,_3=isc.isAn.Object(_1);if(isc.isAn.Array(_1)){_3=false;if(_1.length!=2){_1=null}else{var _4=_1.indexOf(this.$18r);if(_4!=-1)_1[_4]=false;var _5=_1.indexOf(this.$18q);if(_5!=-1)_1[_5]=true;if(!((_1[0]&&!_1[1])||(!_1[0]&&_1[1]))){this.logInfo("Checkbox item created with valueMap:"+_1+"which has no explicit true/false display values. Mapping the first value to true and the second to false.");var _6={};_6[_1[0]]=true;_6[_1[1]]=false;this.valueMap=_1=_6;_3=true}else{_2=true}}}
if(_3){var _7=[],_8=[],_9;for(var _10 in _1){if(_7.length==2){_9=true;break}
var _11=_1[_10];if(_10==this.$18r)_10=false;else if(_10==this.$18q)_10=true;if(_11==this.$18r){_11=_1[_10]=false}else if(_11==this.$18q){_11=_1[_10]=true}
_8[_8.length]=_10;_7[_7.length]=_11}
if(_7.length!=2)_9=true;if(!_9){if((_7[0]&&!_7[1])||(!_7[0]&&_7[1])){}else if((_8[0]&&!_8[1])||(!_8[0]&&_8[1])){_1=_8}else _9=true}
if(_9)_1=null}else if(!_2){_1=null}}
if(_1&&this.allowEmptyValue){if(_2)_1.add(null);else _1[this.emptyValueKey]=null}
return(this.$20b=_1||(this.allowEmptyValue?isc.CheckboxItem.trueFalseNullValueMap:isc.CheckboxItem.trueFalseValueMap))}
,isc.A.$17d=function(){var _1=this.Super("$17d",arguments);if(_1==this.emptyValueKey)_1=null;return _1}
,isc.A.setValueMap=function(){this.$20b=null;return this.Super("setValueMap",arguments)}
,isc.A.init=function(_1,_2,_3,_4){this.invokeSuper(isc.CheckboxItem,"init",_1,_2,_3,_4);if(this.showValueIconOnly==null)this.showValueIconOnly=!this.showLabel;if(this.textAlign==null&&this.align!=null){this.textAlign=this.align}}
,isc.A.setShowLabel=function(_1){this.showLabel=_1;this.showValueIconOnly=!_1;if(this.isDrawn())this.redraw()}
,isc.A.mapValueToDisplay=function(_1,_2,_3,_4){if(this.labelAsTitle)return isc.emptyString;var _5=this.invokeSuper(isc.CheckboxItem,"getTitleHTML",_1,_2,_3,_4);var _6=this.form;if((this.required||this.$11m)&&_6&&_6.hiliteRequiredFields){_5=this.requiredTitlePrefix+_5+this.requiredTitleSuffix}
return _5}
,isc.A.getValueIcon=function(_1){var _2=this.getValueMap();if(!isc.isAn.Array(_2)&&isc.isAn.Object(_2))_1=_2[_1];if(_1)return this.checkedImage;else if(_1===false)return this.uncheckedImage;else{if(this.showUnsetImage!=null){return this.showUnsetImage?this.unsetImage:this.uncheckedImage}
return this.allowEmptyValue?this.unsetImage:this.uncheckedImage}}
,isc.A.getTitleHTML=function(_1,_2,_3){if(this.labelAsTitle)return this.invokeSuper(isc.CheckboxItem,"getTitleHTML",_1,_2,_3);return isc.emptyString}
,isc.A.$15s=function(){return true}
,isc.A.getTextBoxHeight=function(){return null}
,isc.A.getTextBoxCellCSS=function(){var _1=isc.Canvas.$42a;var _2=this.invokeSuper(isc.CheckboxItem,"getTextBoxHeight");if(_2&&isc.isA.Number(_2))
_1+=this.$20a+_2+this.$ph;return _1}
);isc.B._maxIndex=isc.C+11;isc.ClassFactory.defineClass("NativeCheckboxItem","FormItem");isc.A=isc.NativeCheckboxItem.getPrototype();isc.A.textBoxStyle="labelAnchor";isc.A.implementsPromptNatively=isc.screenReader;isc.A.$183="CHECKBOX";isc.A.$125=true;isc.A.showLabel=true;isc.A.$16b={onclick:isc.FormItem.$12y};isc.A=isc.NativeCheckboxItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$20c=["<TABLE CELLSPACING=0 CELLPADDING=0 BORDER=0><TR>","<TD WIDTH=20><INPUT TYPE=",," NAME=",," ID=",,,,,,," handleNativeEvents=false ",,,," TABINDEX=",,(isc.Browser.isMoz?" STYLE='-moz-user-focus:":null),,"></TD>",,"</TR></TABLE>"];isc.A.$20d=["<TD CLASS='",,"' ALIGN=LEFT",,,,," style='",,"'>",,"</TD>"];isc.A.$20e=["<A HREF='javascript:void ",,".boxTitleClick()' ONMOUSEOVER='window.status = \"",,"\"; return true' ONMOUSEOUT='window.status = \"\"; return true' CLASS='",,"' title=\"",,"\">",,"</A>"];isc.B.push(isc.A.getInnerWidth=function(_1,_2,_3,_4){if(!this.showLabel||this.showValueIconOnly){return 20}
return this.invokeSuper(isc.NativeCheckboxItem,"getInnerWidth",_1,_2,_3,_4)}
,isc.A.getElementHTML=function(_1){var _2=this.form.getID(),_3=this.getItemID(),_4=this.$20c,_5=this.getAnchorTitle();_4[2]=this.$183;_4[4]=this.getElementName();_4[6]=this.getDataElementId();_4[7]=this.$155();if(this.value!=null){_4[8]=" VALUE='";_4[9]=this.value;_4[10]="'";if(this.containerWidget&&this.containerWidget.isPrinting){if(_1==this.value)_4[10]+=" CHECKED='true'"}}else{_4[8]=null;_4[9]=null;_4[10]=null}
if(this.isDisabled())_4[11]=" DISABLED";else _4[11]=null;if(this.implementsPromptNatively){if(this.prompt!=null){_4[13]=" TITLE='";_4[14]=this.prompt;_4[15]="'"}else{_4[13]=_4[14]=_4[15]=null}}
var _6=this.$154();_4[17]=_6;if(isc.Browser.isMoz){_4[19]=(_6>0?"normal;'":"ignore;'")}
if(this.showLabel&&!this.showValueIconOnly){var _7=this.getElementWidth(),_8=this.getInnerHeight();if(isc.isA.Number(_7))_7=Math.max(20,_7-20);if(isc.Browser.isSafari&&!this.isDisabled()){var _9=this.$20e;_9[1]=_3;_9[3]=this.prompt;_9[5]=this.getTextBoxStyle();_9[7]=this.prompt;_9[9]=_5;_5=_9.join(isc.emptyString)}
var _10=this.$20d;_10[1]=this.getTextBoxStyle();if(_7!=null){_10[3]=" WIDTH=";_10[4]=_7}else{_10[3]=null;_10[4]=null}
if(_8!=null){_10[5]=" HEIGHT=";_10[6]=_8}else{_10[5]=null;_10[6]=null}
if(this.wrap==false){_10[8]=this.$136}else{_10[8]=null}
_10[10]=_5;_4[21]=_10.join(isc.emptyString)}else{_4[21]=null}
return _4.join(isc.emptyString)}
,isc.A.getOuterElement=function(_1,_2,_3){if(!this.isDrawn())return null;var _4;if(!this.$15s(this.$158)&&!this.showPickerIcon){_4=this.getDataElement()}
return this.invokeSuper(isc.NativeCheckboxItem,"getOuterElement",_1,_2,_3)}
,isc.A.getTitleHTML=function(){return""}
,isc.A.getAnchorTitle=function(_1,_2,_3,_4){return this.invokeSuper(isc.NativeCheckboxItem,"getTitleHTML",_1,_2,_3,_4)}
,isc.A.setElementValue=function(_1){var _2=this.getDataElement();if(!_2)return null;return _2.checked=(_1&&_1!="false")}
,isc.A.getElementValue=function(){var _1=this.getDataElement();if(!_1)return null;return(_1.checked==true)}
,isc.A.boxTitleClick=function(){var _1=this.getDataElement();if(_1)_1.checked=!_1.checked;this.form.elementChanged(this.getItemID())}
);isc.B._maxIndex=isc.C+8;isc.ClassFactory.defineClass("HeaderItem","FormItem");isc.A=isc.HeaderItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.shouldSaveValue=false;isc.A.defaultValue="Header";isc.A.height=20;isc.A.showTitle=false;isc.A.textBoxStyle="headerItem";isc.A.colSpan="*";isc.A.startRow=true;isc.A.endRow=true;isc.A.emptyDisplayValue="&nbsp;";isc.B.push(isc.A.isEditable=function(){return false}
);isc.B._maxIndex=isc.C+1;isc.defineClass("SectionItem","CanvasItem");isc.A=isc.SectionItem.getPrototype();isc.A.shouldSaveValue=false;isc.A.defaultValue="Section Header";isc.A.sectionVisible=true;isc.A.sectionExpanded=true;isc.A.sectionHeaderClass="SectionHeader";isc.A.canCollapse=true;isc.A.autoDestroy=true;isc.A.showTitle=false;isc.A.startRow=true;isc.A.endRow=true;isc.A.colSpan="*";isc.A.width="*";isc.A.height=20;isc.addGlobal("GroupItem",isc.SectionItem);isc.A=isc.SectionItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){if(this.sectionVisible==false)this.sectionExpanded=false;this.Super("init",arguments)}
,isc.A.$18y=function(){var _1=isc.ClassFactory.getClass(this.sectionHeaderClass),_2={autoDraw:false,section:this,title:this.defaultValue,expanded:this.sectionExpanded,layout:this,height:this.height,canCollapse:this.canCollapse,canDrag:false};if(this.baseStyle!=null)_2.baseStyle=this.baseStyle;if(this.printStyleName!=null)_2.printStyleName=this.printStyleName;isc.addProperties(_2,this.canvasDefaults,this.canvasProperties);var _3=_1.create(_2);this.canvas=_3;this.Super("$18y",arguments)}
,isc.A.isEditable=function(){return false}
,isc.A.isExpanded=function(){return this.sectionExpanded==true?true:false}
,isc.A.setValue=function(_1){this.Super("setValue",arguments);if(this.canvas)this.canvas.setTitle(this.getValue())}
,isc.A.sectionHeaderClick=function(){this.cellClick()}
,isc.A.cellClick=function(){if(this.sectionExpanded){this.collapseSection()}else{this.expandSection()}}
,isc.A.expandSection=function(){this.form.$100(this);this.$20f();if(this.itemIds==null){this.logWarn("sectionItem defined with no items or itemIds");return}
for(var i=0;i<this.itemIds.length;i++){var _2=this.itemIds[i],_3=this.form.getItem(_2);if(_3==null){this.logWarn("expandSection: no such item: "+_2);continue}
if(_3.showIf==null&&_3.$20g!=null)_3.showIf=_3.$20g;_3.show(true)}
this.canvas.setExpanded(true);this.sectionExpanded=true;this.form.$10m=true}
,isc.A.$20f=function(){if(this.items!=null&&!this.$20h){this.form.addItems(this.items,this.form.items.indexOf(this)+1);this.itemIds=[];for(var i=0;i<this.items.length;i++){this.itemIds[i]=this.items[i].getFieldName();if(this.itemIds[i]==null){this.logWarn("unable to include item:"+this.items[i]+" with no name in section")}}
this.$20h=true}}
,isc.A.addItem=function(_1,_2){this.form.addItems(_1,this.form.items.indexOf(this)+1+(_2||0));this.itemIds=this.itemIds||[];this.itemIds.add(_1.name)}
,isc.A.removeItem=function(_1){var _2=(isc.isA.Object(_1)?_1.name:_1);this.itemIds.remove(_2);this.form.removeItems(_1)}
,isc.A.getItem=function(_1){return this.form.getItem(_1)}
,isc.A.collapseSection=function(){this.form.$101(this);if(this.itemIds==null){if(this.items==null||this.$20h){this.logWarn("collapseSection with no sectionItem.itemIds");return}}else{for(var i=0;i<this.itemIds.length;i++){var _2=this.itemIds[i],_3=this.form.getItem(_2);if(_3==null){this.logWarn("collapseSection: no such item: "+_2);continue}
if(_3.showIf!=null)_3.$20g=_3.showIf;_3.hide()}}
this.canvas.setExpanded(false);this.sectionExpanded=false;this.form.$10m=true}
);isc.B._maxIndex=isc.C+13;isc.ClassFactory.defineClass("HiddenItem","FormItem");isc.A=isc.HiddenItem.getPrototype();isc.A.showTitle=false;isc.A.cellStyle=null;isc.A.width=0;isc.A.height=0;isc.A.colSpan=0;isc.A.rowSpan=0;isc.A.$125=true;isc.A.canFocus=false;isc.A=isc.HiddenItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.isEditable=function(){return false}
,isc.A.getInnerHTML=function(_1){var _2=isc.StringBuffer.concat("<INPUT ID='",this.getDataElementId(),"' TYPE=HIDDEN NAME=",this.getElementName(),">");return _2.toString()}
,isc.A.getRowSpan=function(){return 0}
,isc.A.getColSpan=function(){return 0}
,isc.A.shouldShowTitle=function(){return false}
,isc.A.getErrorHTML=function(_1){this.logError("Error in hidden field '"+this.getFieldName()+"':\r  "+_1);return null}
,isc.A.isStartRow=function(){return false}
,isc.A.isEndRow=function(){return false}
);isc.B._maxIndex=isc.C+8;isc.ClassFactory.defineClass("StaticTextItem","FormItem");isc.A=isc.StaticTextItem.getPrototype();isc.A.height=null;isc.A.width=null;isc.A.wrap=true;isc.A.textBoxStyle="staticTextItem";isc.A.outputAsHTML=false;isc.A.emptyDisplayValue="&nbsp;";isc.A=isc.StaticTextItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.mapValueToDisplay=function(_1,_2,_3,_4,_5){var _6=this.invokeSuper(isc.StaticTextItem,"mapValueToDisplay",_1,_2,_3,_4,_5);var _7=this.outputAsHTML||this.asHTML;if(_7&&(_1==null||_1==isc.emptyString)&&_6==this.$54t)
{_7=false}
if(isc.isA.String(_6)&&_7){_6=_6.asHTML()}
return _6}
,isc.A.isEditable=function(){return false}
);isc.B._maxIndex=isc.C+2;isc.ClassFactory.defineClass("LinkItem","StaticTextItem");isc.A=isc.LinkItem.getPrototype();isc.A.wrap=false;isc.A.canFocus=true;isc.A=isc.LinkItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$14g=["<DIV ID='",,"' "+isc.DynamicForm.$89+"='",,"' "+isc.DynamicForm.$9a+"='"+isc.DynamicForm.$9c,"' CLASS='",,"' STYLE='",,"' onclick='if(window.",,") return ",,".$30i(event);","'>",,"</DIV>"];isc.B.push(isc.A.$15m=function(){return false}
,isc.A.$20i=function(){if(!this.isDrawn())return null;return(isc.Element.get(this.getID()+"$20j"))}
,isc.A.getFocusElement=function(){return this.$20i()}
,isc.A.$30i=function(_1){var _2=(this.destroyed||!this.isDrawn()||!this.isVisible());if(!_2){_2=isc.EH.targetIsMasked(this.containerWidget);if(_2&&(this.form!=this.containerWidget)){_2=isc.EH.targetIsMasked(this.form)}}
if(this.target=="javascript"){_2=true;this.handleClick()}
if(_2){if(!isc.Browser.isIE){_1.preventDefault()}
return false}
return true}
,isc.A.getElementHTML=function(_1){var _2=this.getLinkHTML(_1);var _3=this.$14g;_3[1]=this.$15g();_3[3]=this.getID();_3[6]=this.getTextBoxStyle();_3[8]=this.getTextBoxCSS();_3[10]=this.getID();_3[12]=this.getID();_3[15]=_2;return _3.join(isc.emptyString)}
,isc.A.getLinkHTML=function(_1){var _2=this.$xq(this._value);if(this.showValueIconOnly)return _2;if(_1!=null)_1=isc.iscToLocaleString(_1);if(_1==null)_1=isc.emptyString;var _3=this.linkTitle;if(_3==null)_3=_1;var _4=this.target;if(_4=="javascript"){_1="javascript:void"}
_1=isc.Canvas.linkHTML(_1,_3,_4,(this.getID()+"$20j"),this.getGlobalTabIndex(),this.accessKey)
if(_2!=null)_1=_2+_1;return _1}
,isc.A.setElementValue=function(_1){if(this.isDrawn()){var _2=this.$15h();if(_2)_2.innerHTML=this.getLinkHTML(_1);this.$159()}}
,isc.A.setLinkTitle=function(_1){this.linkTitle=_1;this.redraw()}
);isc.B._maxIndex=isc.C+8;isc.ClassFactory.defineClass("PasswordItem","TextItem");isc.A=isc.PasswordItem.getPrototype();isc.A.$183="PASSWORD";isc.ClassFactory.defineClass("RadioGroupItem","ContainerItem");isc.A=isc.RadioGroupItem.getPrototype();isc.A.itemHeight=20;isc.A.vertical=true;isc.A.prompt=null;isc.A.textBoxStyle="labelAnchor";isc.A=isc.RadioGroupItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.$11d=function(){return false}
,isc.A.setItems=function(){var _1=this.getValueMap();if(!this.itemCache)this.itemCache={};var _2=[];if(isc.isAn.Array(_1)){for(var i=0;i<_1.length;i++){var _4=_1[i];_2.add(this.$20k(_4,_4))}}else{for(var _4 in _1){var _5=_1[_4];_2.add(this.$20k(_5,_4))}}
return this.Super("setItems",[_2])}
,isc.A.$20k=function(_1,_2){var _3=this.itemCache[_2+"|"+_1];if(_3){delete _3._value;delete _3.hasFocus;delete _3.disabled}else{var _4;if(this.itemPrompt){var _5=new RegExp("\\*","g");_4=this.itemPrompt.replace(_5,_1)}
var _6={type:"radio",name:"$540"+_2,value:_2,getElementName:function(){return this.parentItem.getElementName()},title:_1,prompt:_4,height:this.itemHeight,$8l:this.vertical,textBoxStyle:this.textBoxStyle,wrap:this.wrap,updateValue:function(){this.parentItem.updatePreviousSelection(this.value);return this.Super("updateValue",arguments)},shouldSaveValue:false};isc.addProperties(_6,this.itemProperties);_3=this.itemCache[_2+"|"+_1]=isc.FormItemFactory.makeItem(_6)}
return _3}
,isc.A.getItemValue=function(_1){var _2=_1.value;if(_2==this.getValue())return _2;return _1.unselectedValue}
,isc.A.itemForValue=function(_1){return this["$540"+_1]}
,isc.A.setValueDisabled=function(_1,_2){var _3=this.itemForValue(_1);if(_3&&this.items.contains(_3)){_3.setDisabled(_2)}}
,isc.A.getInnerHTML=function(_1){this.setItems();return this.Super("getInnerHTML",arguments)}
,isc.A.isEditable=function(){return true}
,isc.A.setValue=function(_1){this.$17g=true;var _2=(this.valueMap==null)||(isc.isAn.Array(this.valueMap)?!this.valueMap.contains(_1):!isc.propertyDefined(this.valueMap,_1));if(_1==null||_2){_1=this.getDefaultValue()}
if(this.items!=null){var _3=this.itemForValue(_1);if(_3!=null){this.itemForValue(_1).setValue(_1)}
if(this._value!=null&&this._value!=_1){var _4=this.itemForValue(this._value);if(_4)_4._value=null}}
this.saveValue(_1)}
,isc.A.updatePreviousSelection=function(_1){var _2=this.getValue();if(isc.isA.String(_1))_2=_2+"";if(_2==null||_1==_2||this.itemForValue(_2)==null)
{return}
this.itemForValue(_2).updateValue()}
,isc.A.updateValue=function(){var _1;for(var i=0;i<this.items.length;i++){_1=this.items[i].getValue();if(_1!=null)break}
if(_1==this._value)return;if(this.handleChange(_1,this._value)==false)return;_1=this.$17n;this.saveValue(_1);this.handleChanged(_1)}
,isc.A.setValueMap=function(_1){this.Super("setValueMap",arguments);this.redraw()}
,isc.A.getHeight=function(){var _1=this.getValueMap(),_2=0;if(isc.isAn.Array(_1)){_2=_1.length}else{for(var _3 in _1){_2++}}
return _2*this.itemHeight}
);isc.B._maxIndex=isc.C+13;isc.ClassFactory.defineClass("RadioItem","NativeCheckboxItem");isc.A=isc.RadioItem.getPrototype();isc.A.$183="RADIO";isc.A.value=true;isc.A.defaultValue=null;isc.A=isc.RadioItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.setElementValue=function(_1){var _2=this.getDataElement();if(!_2)return null;if(isc.isA.String(this.value))_1=(_1+"");return _2.checked=(this.value==_1)}
,isc.A.getElementValue=function(){var _1=this.getDataElement(),_2=this.value,_3=this.unselectedValue;if(!_1)return _3;return(_1.checked?_2:_3)}
,isc.A.boxTitleClick=function(){var _1=this.getDataElement();if(_1&&!_1.checked){_1.checked=true;this.form.elementChanged(this.getItemID())}}
,isc.A.mapValueToDisplay=function(_1){return _1}
,isc.A.mapDisplayToValue=function(_1){return _1}
);isc.B._maxIndex=isc.C+5;isc.ClassFactory.defineClass("ResetItem","ButtonItem");isc.A=isc.ResetItem.getPrototype();isc.A.title="Reset";isc.A=isc.ResetItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.handleClick=function(){if(this.Super("handleClick",arguments)==false)return false;this.form.resetValues()}
);isc.B._maxIndex=isc.C+1;if(isc.ListGrid){isc.defineClass("DateItem","ContainerItem");isc.A=isc.DateItem;isc.A.mapCache={};isc.A.TEXT_FIELD={name:"dateTextField",type:"text",changeOnBlur:true,changeOnKeypress:false,blur:function(){if(this.parentItem)this.parentItem.updateValue()},shouldSaveValue:false,getInnerWidth:function(){if(this.parentItem)return this.parentItem.getTextBoxWidth();return this.Super("getInnerWidth",arguments)}};isc.A.DAY_SELECTOR={name:"daySelector",prompt:"Choose a day",type:"select",valueMap:"this.parentItem.getDayOptions()",shouldSaveValue:false,saveValue:function(){this.Super("saveValue",arguments);this.parentItem.updateValue()},cssText:"padding-left:3px;",width:45};isc.A.MONTH_SELECTOR={name:"monthSelector",prompt:"Choose a month",type:"select",valueMap:"this.parentItem.getMonthOptions()",shouldSaveValue:false,saveValue:function(){this.Super("saveValue",arguments);this.parentItem.updateValue()},width:55};isc.A.YEAR_SELECTOR={name:"yearSelector",prompt:"Choose a year",type:"select",valueMap:"this.parentItem.getYearOptions()",shouldSaveValue:false,saveValue:function(){this.Super("saveValue",arguments);this.parentItem.updateValue()},cssText:"padding-left:3px;",width:60};isc.A.DAY_MONTH_YEAR="DMY";isc.A.MONTH_DAY_YEAR="MDY";isc.A.YEAR_MONTH_DAY="YMD";isc.A.DAY_MONTH="DM";isc.A.MONTH_DAY="MD";isc.A.YEAR_MONTH="YM";isc.A.MONTH_YEAR="MY";isc.A.DEFAULT_START_DATE=new Date(1995,0,1);isc.A.DEFAULT_END_DATE=new Date(2015,11,31);isc.A.DEFAULT_CENTURY_THRESHOLD=25;isc.A.chooserWidth=150;isc.A.chooserHeight=171;isc.A=isc.DateItem.getPrototype();isc.A.width=150;isc.A.cellPadding=0;isc.A.pickerConstructor="DateChooser";isc.A.pickerProperties={width:isc.DateItem.chooserWidth,height:isc.DateItem.chooserHeight,border:"1px solid black;",showCancelButton:true,autoHide:true};isc.A.textAlign=isc.Canvas.RIGHT;isc.A.maskDateSeparator="/";isc.A.enforceDate=false;isc.A.invalidDateStringMessage="Invalid date";isc.A.showPickerIcon=true;isc.A.pickerIconWidth=20;isc.A.pickerIconHeight=20;isc.A.pickerIconSrc="[SKIN]/DynamicForm/DatePicker_icon.gif";isc.A.pickerIconHSpace=3;isc.A.pickerIconPrompt="Show Date Chooser";isc.A.pickerIconProperties={};isc.A.startDate=isc.DateItem.DEFAULT_START_DATE;isc.A.endDate=isc.DateItem.DEFAULT_END_DATE;isc.A.centuryThreshold=isc.DateItem.DEFAULT_CENTURY_THRESHOLD;isc.A=isc.DateItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$73c={"MDY":"[01][0-9]/[0-3]#/####","DMY":"[0-3]#/[01][0-9]/####","YMD":"####/[01][0-9]/[0-3]#"};isc.A.$73d="[0-2][0-9]:[0-6][0-9]";isc.A.$73r={"MDY":"toUSShortDate","DMY":"toEuropeanShortDate","YMD":"toJapanShortDate"};isc.A.$73e="/";isc.A.$73f=/\//g;isc.A.selectorPadding=2;isc.A.useCustomTimezone=false;isc.B.push(isc.A.init=function(){if(this.useTextField==null)this.useTextField=this.useMask||false;if(this.dateFormatter!=null){this.logInfo("Configuration block for this item has an explicitly specified "+"'dateFormatter' value:"+this.dateFormatter+". This will be used instead of "+"the specified 'displayFormat' attribute for this item.");this.displayFormat=this.dateFormatter}
return this.Super("init",arguments)}
,isc.A.getSelectorFormat=function(){if(this.selectorFormat){return this.selectorFormat}else if(this.inputFormat&&isc.isA.String(this.inputFormat)){return this.inputFormat}else{var _1=Date.getInputFormat();if(isc.isA.String(_1))return _1;this.logInfo("DateItem selectorFormat unspecified - assuming US format");return"MDY"}}
,isc.A.setItems=function(_1){var _2=isc.DateItem,_3=this.getSelectorFormat();if(_1!=null&&_1.length!=0){this.logWarn("setItems() called for dateItem with itemList:"+_1+" - ignoring, and making use of default date fields")}
_1=this.items=[];if(this.useTextField){var _4={};if(this.useMask){var _5=this.getInputFormat();if(!_5)_5="MDY";var _6=this.$73c[_5];var _7=this.maskDateSeparator||this.$73e;if(_7!=this.$73e){_6=_6.replace(this.$73f,_7)}
if(isc.isA.DateTimeItem(this)){_6+=" "+this.$73d}
_4.mask=_6;_4.maskSaveLiterals=true;_4.maskOverwriteMode=true;Number.$et="0";if(this.inputFormat)
this.displayFormat=this.$73r[_5]}
var _8=isc.addProperties({textAlign:this.textAlign},_2.TEXT_FIELD,this.textFieldProperties,_4);if(this.height&&(!this.textFieldProperties||!this.textFieldProperties.height))
{_8.height=this.getTextBoxHeight()}
_1.add(_8);var _9;this.daySelector=this.yearSelector=this.monthSelector=_9}else{for(var i=0;i<_3.length;i++){var _11=_3.charAt(i);var _12,_13,_14;if(_11=="D"){var _12;if(this.daySelectorProperties!=null){_12=isc.addProperties({},_2.DAY_SELECTOR,this.daySelectorProperties)}else{_12=isc.addProperties({},_2.DAY_SELECTOR)}
_1.add(_12)}else if(_11=="M"){var _13;if(this.monthSelectorProperties!=null){_13=isc.addProperties({},_2.MONTH_SELECTOR,this.monthSelectorProperties)}else{_13=isc.addProperties({},_2.MONTH_SELECTOR)}
_1.add(_13)}else if(_11=="Y"){var _14;if(this.yearSelectorProperties!=null){_14=isc.addProperties({},_2.YEAR_SELECTOR,this.yearSelectorProperties)}else{_14=isc.addProperties({},_2.YEAR_SELECTOR)}
_1.add(_14)}}}
this.Super("setItems",[_1])}
,isc.A.getInnerWidth=function(){if(this.useTextField){return this.Super("getInnerWidth",arguments)}
var _1=0,_2=0;if(this.daySelector){_2+=1;_1+=this.daySelector.width}
if(this.monthSelector){_2+=1;_1+=this.monthSelector.width}
if(this.yearSelector){_2+=1;_1+=this.yearSelector.width}
if(this.showPickerIcon)_1+=this.getPickerIconWidth();if(_2>0)_1+=(_2-1)*this.selectorPadding;return _1}
,isc.A.isEditable=function(){return true}
,isc.A.setValue=function(_1){this.$17g=true;var _2=false;if(_1==null){_1=this.getDefaultValue();_2=true}
var _3,_4;if(isc.is.emptyString(_1))_1=null;if(_1==null){_4=true;_3=null}else{_3=this.parseDate(_1);if(_3==null){_4=true;_3=_1}}
if(_4){var _5;if(!this.useTextField){_5=true}else if(this.enforceDate&&_1!=null){var _6=this.dateTextField;_5=!this.$20m||!_6||(_6.getValue()!=_1)}
if(_5){this.logInfo("dateItem.setValue(): invalid date passed: '"+_1+"'.  Ignoring this value. Non date values are only supported "+" for dateItems where useTextField is true and enforceDate is false.");return false}}
if(!_4&&this.$20m){delete this.$20m;this.clearErrors();this.redraw()}
this.saveValue(_3,_2);this.$20l=true;if(this.useTextField){var _7=_4?_3:this.formatDate(_3);if(this.dateTextField)this.dateTextField.setValue(_7)}
if(this.daySelector)this.daySelector.setValue(_3.getDate());if(this.monthSelector)this.monthSelector.setValue(_3.getMonth());if(this.yearSelector)this.yearSelector.setValue(_3.getFullYear());delete this.$20l;return true}
,isc.A.$17e=function(_1){var _2=this.$15b();if(_2!=null){if(isc.isA.Date(_1))_2.value=_1.toDBDate();else _2.value=_1}}
,isc.A.getCellHeight=function(){var _1=this.Super("getCellHeight",arguments);if(isc.Browser.isIE&&this.useTextField&&isc.isA.Number(_1))_1+=2;return _1}
,isc.A.elementChanged=function(){return}
,isc.A.updateValue=function(){if(this.$20l)return;this.$20l=true;var _1;if(this.useTextField){this.dateTextField.updateValue();var _2=this.dateTextField.getValue(),_3;if(_2==isc.emptyString||_2==null)_1=null;else{_1=this.parseDate(_2);if(_1==null){_3=true;_1=_2}else{var _4=this.formatDate(_1);if(_2!=_4){this.dateTextField.setValue(_4)}}}
if(this.enforceDate){if(this.$20m&&!_3){delete this.$20m;this.clearErrors();this.redraw()}else if(_3){this.logWarn("Invalid date string entered in date text field :"+_1);if(!this.$20m){this.$20m=true;this.setError(this.invalidDateStringMessage);this.redraw()}}}}else{_1=(this._value||this.getDefaultValue());_1=_1.duplicate();var _5,_6,_7;_5=(this.daySelector?this.daySelector.getValue():_1.getDate());_1.setDate(1);if(this.yearSelector){_7=this.yearSelector.getValue()
_1.setYear(_7)}
if(this.monthSelector){_6=this.monthSelector.getValue();_1.setMonth(_6)}
_1.setDate(_5);if(_6!=_1.getMonth()){_5=_5-_1.getDate();if(this.daySelector)this.daySelector.setValue(_5);_1.setMonth(_6);_1.setDate(_5)}}
delete this.$20l;if(this.handleChange(_1,this._value)==false)return;_1=this.$17n;this.saveValue(_1);this.handleChanged(_1)}
,isc.A.resetValue=function(){var _1=this.form.$10s[this.getFieldName()];if(isc.isA.Date(_1)&&isc.isA.Date(this._value))
_1=this._value.setTime(_1.getTime());this.setValue(_1)}
,isc.A.getItemValue=function(_1,_2){if(isc.isAn.emptyObject(_2))_2=null;var _3=isc.isA.Date(_2),_4=isc.isA.Date(this._value);if(_2==this._value||(_3&&_4&&(Date.compareDates(_2,this._value)==0)))
{return _1.getValue()}
if(_1==this.dateTextField)return _3?this.formatDate(_2):_2;else if(_1==this.daySelector)return _3?_2.getDate():null;else if(_1==this.monthSelector)return _3?_2.getMonth():null;else if(_1==this.yearSelector)return _3?_2.getFullYear():null}
,isc.A.getDisplayValue=function(){var _1=this.getValue();if(!isc.isA.Date(_1))return this.Super("getDisplayValue",arguments);if(this.useTextField||!this.items){return this.formatDate(_1)}else{if(!this.isDrawn()){if(this.yearSelector)this.yearSelector.setValue(_1.getFullYear());if(this.monthSelector)this.monthSelector.setValue(_1.getMonth());if(this.daySelector)this.daySelector.setValue(_1.getDate())}
return this.items.map("getDisplayValue").join(" ")}}
,isc.A.getDefaultValue=function(){var _1=this.Super("getDefaultValue");if(!isc.isA.Date(_1)){var _2=this.parseDate(_1);if(isc.isA.Date(_2))_1=_2;else if(!this.useTextField||this.enforceDate){var _3;if(_1!=null){this.logWarn("Default DateItem value provided as:"+_1+". This is not recognized as a valid date - defaulting to a new date");_3=this.defaultValue==_1}
if(!this.useTextField)_1=this.$603();if(_3)this.defaultValue=_1}}
return _1}
,isc.A.$603=function(){var _1=new Date();_1.setHours(0);_1.setMinutes(0);_1.setSeconds(0);_1.setMilliseconds(0);return _1}
,isc.A.getStartDate=function(){var _1=this.parseDate(this.startDate);if(!isc.isA.Date(_1)){this.logWarn("startDate was not in valid date format - using default start date");_1=isc.DateItem.DEFAULT_START_DATE}
return _1}
,isc.A.getEndDate=function(){var _1=this.parseDate(this.endDate);if(!isc.isA.Date(_1)){this.logWarn("endDate was not in valid date format - using default end date");_1=isc.DateItem.DEFAULT_END_DATE}
return _1}
,isc.A.$kk=function(){if(this.canFocus!=null)return this.canFocus;return true}
,isc.A.focusInItem=function(){if(!this.isVisible())return;if(this.useTextField){if(this.dateTextField)this.dateTextField.focusInItem()}else{var _1=this.getSelectorFormat(),_2=_1.charAt(0);if(_2=="D"&&this.daySelector)this.daySelector.focusInItem();if(_2=="M"&&this.monthSelector)this.monthSelector.focusInItem();if(_2=="Y"&&this.yearSelector)this.yearSelector.focusInItem()}}
,isc.A.setSelectionRange=function(_1,_2){if(this.dateTextField)return this.dateTextField.setSelectionRange(_1,_2)}
,isc.A.getSelectionRange=function(){if(this.dateTextField)return this.dateTextField.getSelectionRange()}
,isc.A.selectValue=function(){if(this.dateTextField)return this.dateTextField.selectValue()}
,isc.A.deselectValue=function(_1){if(this.dateTextField)return this.dateTextField.deselectValue()}
,isc.A.getDayOptions=function(){var _1=this.getStartDate(),_2=this.getEndDate();var _3=1,_4=31;if(_1.getYear()==_2.getYear()&&_1.getMonth()==_2.getMonth())
{_3=_1.getDate()
_4=_2.getDate()}
var _5="day."+_3+"."+_4;if(isc.DateItem.mapCache[_5])return isc.DateItem.mapCache[_5];var _6=isc.DateItem.mapCache[_5]=[];for(var i=_3;i<=_4;i++)_6[i-_3]=i;return _6}
,isc.A.getMonthOptions=function(){var _1=this.getStartDate(),_2=this.getEndDate();var _3=0,_4=11;if(_1.getYear()==_2.getYear()){_3=_1.getMonth()
_4=_2.getMonth()}
var _5="month."+_3+"."+_4;if(isc.DateItem.mapCache[_5])return isc.DateItem.mapCache[_5];var _6=isc.DateItem.mapCache[_5]={};var _7=Date.getShortMonthNames();for(;_3<=_4;_3++){_6[_3]=_7[_3]}
return _6}
,isc.A.getYearOptions=function(){var _1=this.getStartDate().getFullYear(),_2=this.getEndDate().getFullYear();var _3="year."+_1+"."+_2;if(isc.DateItem.mapCache[_3])return isc.DateItem.mapCache[_3];var _4=isc.DateItem.mapCache[_3]=[];for(var i=_1;i<=_2;i++){_4[i-_1]=i}
return _4}
,isc.A.parseDate=function(_1,_2){if(_2==null)_2=this.getInputFormat();return Date.parseInput(_1,_2,this.centuryThreshold,true,this.useCustomTimezone)}
,isc.A.formatDate=function(_1){return isc.isA.Date(_1)?_1.toShortDate(this.displayFormat,this.useCustomTimezone):_1}
,isc.A.getInputFormat=function(){if(this.inputFormat)return this.inputFormat;if(this.displayFormat){return Date.mapDisplayFormatToInputFormat(this.displayFormat)}
return null}
,isc.A.getPickerIcon=function(_1,_2,_3,_4){var _5=this.invokeSuper(isc.DateItem,"getPickerIcon",_1,_2,_3,_4);if(_5.prompt==null)_5.prompt=this.pickerIconPrompt;return _5}
,isc.A.showPicker=function(){if(!this.picker)this.picker=isc.DateChooser.getSharedDateChooser();var _1=this.picker;var _2=_1.callingFormItem;if(_2!=this){if(_2)_2.ignore(_1,"dataChanged");this.observe(_1,"dataChanged","observer.pickerDataChanged(observed)");_1.callingFormItem=this;_1.callingForm=this.form;_1.locatorParent=this.form}
return this.Super("showPicker",arguments)}
,isc.A.getPickerRect=function(){var _1=this.getPageLeft(),_2=this.getPageTop(),_3=isc.DateItem.chooserWidth+3,_4=isc.DateItem.chooserHeight+3;_1+=(this.getVisibleWidth()-(this.getPickerIconWidth()/2))-
(_3/ 2);_2+=(this.getPickerIconHeight()/2)-(_4/ 2);return[_1,_2]}
,isc.A.pickerDataChanged=function(_1){var _2=_1.getData();var _3=_2.getFullYear(),_4=_2.getMonth(),_5=_2.getDate();this.$20l=true;if(this.useTextField){this.dateTextField.setValue(this.formatDate(_2))}else{var _2=this._value||this.getDefaultValue(),_6;if(this.yearSelector)this.yearSelector.setValue(_3);else{_2.setFullYear(_3);_6=true}
if(this.monthSelector)this.monthSelector.setValue(_4);else{_2.setMonth(_4-1);_6=true}
if(this.daySelector)this.daySelector.setValue(_5);else{_2.setDate(_5);_6=true}
if(_6){this._value=_2}}
this.$20l=false;this.updateValue();if(!this.hasFocus)this.focusInItem()}
,isc.A.propertyChanged=function(_1){if(_1=="useTextField"||_1=="useMask")this.setItems()}
);isc.B._maxIndex=isc.C+34}
if(isc.ListGrid){isc.defineClass("DateTimeItem","DateItem");isc.A=isc.DateTimeItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.useTextField=true;isc.A.useCustomTimezone=true;isc.B.push(isc.A.formatDate=function(_1){if(!isc.isA.Date(_1))return _1;return _1.toShortDateTime(this.displayFormat,this.useCustomTimezone)}
);isc.B._maxIndex=isc.C+1}
isc.ClassFactory.defineClass("SpacerItem","FormItem");isc.A=isc.SpacerItem.getPrototype();isc.A.shouldSaveValue=false;isc.A.showTitle=false;isc.A.width=20;isc.A.height=20;isc.A.showHint=false;isc.A.showIcons=false;isc.A=isc.SpacerItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.isEditable=function(){return false}
,isc.A.getElementHTML=function(_1){return isc.Canvas.spacerHTML(this.width,this.height)}
,isc.A.shouldShowTitle=function(){return false}
);isc.B._maxIndex=isc.C+3;isc.ClassFactory.defineClass("RowSpacerItem","SpacerItem");isc.A=isc.RowSpacerItem.getPrototype();isc.A.showTitle=false;isc.A.colSpan="*";isc.A.startRow=true;isc.A.endRow=true;isc.A.width=20;isc.A.height=20;isc.ClassFactory.defineClass("SubmitItem","ButtonItem");isc.A=isc.SubmitItem.getPrototype();isc.A.title="Submit";isc.A=isc.SubmitItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.handleClick=function(){if(this.Super("handleClick",arguments)==false)return false;this.form.submit()}
);isc.B._maxIndex=isc.C+1;isc.ClassFactory.defineClass("TextAreaItem","FormItem");isc.A=isc.TextAreaItem;isc.A.OFF="OFF";isc.A.SOFT="SOFT";isc.A.VIRTUAL="SOFT";isc.A.ON="HARD";isc.A.HARD="HARD";isc.A.PHYSICAL="HARD";isc.A=isc.TextAreaItem.getPrototype();isc.A.wrap=isc.TextAreaItem.VIRTUAL;isc.A.width=150;isc.A.height=100;isc.A.textBoxStyle="textItem";isc.A.redrawOnShowIcon=false;isc.A.$125=true;isc.A.$15i=true;isc.A.$185=null;isc.A.lineBreakValue="\n";isc.A.iconVAlign=isc.Canvas.TOP;isc.A.$16b={onmousedown:(isc.Browser.isIE?function(){var _1=this,_2=isc.DynamicForm.$mu(_1),_3=_2.item;if(_3)_3.$179()}:null)};isc.A.supportsSelectionRange=true;isc.A=isc.TextAreaItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.applyStaticTypeFormat=false;isc.B.push(isc.A.getTextBoxCellCSS=function(){return this.$136}
,isc.A.$143=function(){return isc.Browser.isStrict}
,isc.A.$429=function(){return!isc.Browser.isIE}
,isc.A.getElementHTML=function(_1,_2){var _3=this.form,_4=_3.getID(),_5=this.getItemID(),_6=isc.StringBuffer.create(),_7=this.$xq(_2);if(_7!=null)_6.append(_7);if(!this.showValueIconOnly){_6.append("<TEXTAREA NAME=",this.getElementName()," ID=",this.getDataElementId(),this.$155(),this.getElementStyleHTML(),(this.isDisabled()?" DISABLED ":""),(this.$17h()!="native"?" AUTOCOMPLETE=OFF ":""),((isc.Browser.isMoz||isc.Browser.isSafari)?(this.getBrowserSpellCheck()?" spellcheck=true":" spellcheck=false"):null)," WRAP=",this.wrap," TABINDEX=",this.$154(),(this.showTitle==false&&this.accessKey!=null?" ACCESSKEY="+this.accessKey:""),(this.$429?" ONINPUT='"+this.getID()+".$43g()'":null),(this.readOnly||this.isInactiveHTML()?" READONLY=TRUE":null)," handleNativeEvents=false>",(this.isInactiveHTML()?_1:null),"</TEXTAREA>")}
return _6.release()}
,isc.A.$12v=function(_1,_2){var _3=this.Super("$12v",arguments);this.$66s();return _3}
,isc.A.$12x=function(_1,_2){var _3=this.Super("$12x",arguments);if(this.changeOnBlur)this.form.elementChanged(this);else{var _4=this.getElementValue();if(this.mapDisplayToValue){_4=this.mapDisplayToValue(_4)}
if(this._value!=_4)this.form.elementChanged(this)}
if(this.showHintInField){var _5;var _6=this.getElementValue();if(_6===_5||_6==null||isc.is.emptyString(_6)){this.$66t()}}
return _3}
,isc.A.getElementStyleHTML=function(){var _1=this.getTextBoxWidth(),_2=this.getTextBoxHeight();return isc.StringBuffer.concat(" CLASS='"+this.getTextBoxStyle(),(isc.Browser.isMoz&&isc.isA.String(this.wrap)&&this.wrap.toLowerCase()!="off"?"' ROWS=10 COLS=10":"'")," STYLE='",this.getElementCSSText(_1,_2),"' ")}
,isc.A.getElementCSSText=function(_1,_2){return isc.StringBuffer.concat((isc.Browser.isIE?"margin-top:-1px;margin-bottom:-1px;margin-left:0px;margin-right:0px;":"margin:0px;"),(isc.isA.Number(_1)?"WIDTH:"+_1+"px;":""),(isc.isA.Number(_2)?"HEIGHT:"+_2+"px;":""),(this.textAlign?"text-align:"+this.textAlign+";":""),(isc.Browser.isMoz?"-moz-user-focus:"+(this.$154()>0?"normal;":"ignore;"):""))}
,isc.A.mapValueToDisplay=function(_1,_2,_3,_4,_5){var _6=this.invokeSuper(isc.TextAreaItem,"mapValueToDisplay",_1,_2,_3,_4,_5);if(_6==null)_6=isc.emptyString;return _6}
,isc.A.mapDisplayToValue=function(_1){if(!this.applyStaticTypeFormat&&this.parseEditorValue!=null){return this.parseEditorValue(_1,this.form,this)}
var _2=this.$17d(_1);if(isc.is.emptyString(_2))_2=this.$185;return _2}
,isc.A.setValue=function(_1){this.$66s();var _2;if(_1!==_2&&(_1==null||isc.is.emptyString(_1)))
this.$185=_1;delete this.$20n;_1=this.Super("setValue",arguments);if(!this.hasFocus&&this.showHint&&this.showHintInField&&this.getHint()){if(_1===_2||_1==null||isc.is.emptyString(_1)){this.$66t()}}
return _1}
,isc.A.updateValue=function(){this.$20n=true;return this.Super("updateValue",arguments)}
,isc.A.getValue=function(){var _1=this.Super("getValue",arguments);if(this.$20n&&isc.isA.String(_1)){if(!this.$20o)
this.$20o=new RegExp("(\\r\\n|[\\r\\n])","g");_1=""+_1;_1=_1.replace(this.$20o,this.lineBreakValue)}
return _1}
,isc.A.getScrollHeight=function(){var _1=this.$15h();if(_1==null)return this.getHeight();return _1.scrollHeight}
,isc.A.getScrollWidth=function(){var _1=this.$15h();if(_1==null)return this.getWidth();return _1.scrollWidth}
,isc.A.$20p=function(){var _1=this.$15h();return _1&&_1.scrollWidth>_1.clientWidth}
,isc.A.$20q=function(){var _1=this.$15h();return _1&&_1.scrollHeight>_1.clientHeight}
,isc.A.getScrollTop=function(){var _1=this.$15h();if(_1==null)return 0;return _1.scrollTop}
,isc.A.getScrollLeft=function(){var _1=this.$15h();if(_1==null)return 0;return _1.scrollLeft}
,isc.A.scrollTo=function(_1,_2){var _3=this.$15h();if(_3==null)return;if(_1!=null)_3.scrollLeft=_1;if(_2!=null)_3.scrollTop=_2}
,isc.A.scrollToTop=function(){this.scrollTo(null,0)}
,isc.A.scrollToBottom=function(){var _1=this.getScrollHeight()-this.getInnerHeight();if(_1>=0){if(this.$20p())_1+=this.form.getScrollbarSize();this.scrollTo(null,_1)}}
);isc.B._maxIndex=isc.C+22;isc.ClassFactory.defineClass("AutoFitTextAreaItem","TextAreaItem");isc.A=isc.AutoFitTextAreaItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.supportsSelectionRange=true;isc.B.push(isc.A.getTestBox=function(_1){var _2=this.mapValueToDisplay(this.getValue());var _3=isc.AutoFitTextAreaItem;if(!_3.$552){_3.$552=isc.Canvas.create({autoDraw:true,overflow:"hidden",left:0,top:-100,contents:["<textarea ID='isc_autoFitTextArea_sizeTester'","style='overflow:hidden;",(isc.Browser.isIE?"margin-top:-1px;margin-bottom:-1px;margin-left:0px;margin-right:0px;":"margin:0px;"),"'></textarea>"].join("")})}
var _4=isc.Element.get("isc_autoFitTextArea_sizeTester");if(_3.currentItem!=this||_1){_4.className=this.getTextBoxStyle();if(isc.Browser.isMoz){if(isc.isA.String(this.wrap)&&this.wrap.toLowerCase()!="off"){_4.rows=10;_4.cols=10}else{_4.rows="";_4.cols=""}}
_4.setAttribute("wrap",this.wrap);_4.style.width=this.getTextBoxWidth();_4.style.height=this.getTextBoxHeight();_4.style.textAlign=this.textAlign||"";_4.cssText=this.getElementCSSText(this.getTextBoxWidth(),this.getTextBoxHeight());_3.currentItem=this}
_4.value=_2;var _5=_4.scrollHeight;return _4}
,isc.A.getScrollHeight=function(_1){var _2=this.getTestBox(_1);return _2.scrollHeight}
,isc.A.getScrollWidth=function(_1){var _2=this.getTestBox(_1);return _2.scrollWidth}
,isc.A.getElementCSSText=function(_1,_2){var _3=this.Super("getElementCSSText",arguments);_3+="overflow:hidden;"
return _3}
,isc.A.$553=function(){if(this.$554!=null)return this.$554;var _1=this.getDataElement();if(!_1)return 0;var _2=parseInt(isc.Element.getComputedStyleAttribute(_1,"paddingLeft")),_3=parseInt(isc.Element.getComputedStyleAttribute(_1,"paddingRight")),_4=(isc.isA.Number(_2)?_2:0)+(isc.isA.Number(_3)?_3:0);this.$554=_4;return _4}
,isc.A.$555=function(){if(this.$556!=null)return this.$556;var _1=this.getDataElement();if(!_1)return 0;if(isc.Browser.isIE&&_1.currentStyle==null)return 0;var _2=parseInt(isc.Element.getComputedStyleAttribute(_1,"paddingTop")),_3=parseInt(isc.Element.getComputedStyleAttribute(_1,"paddingBottom")),_4=(isc.isA.Number(_2)?_2:0)+(isc.isA.Number(_3)?_3:0);this.$556=_4;return _4}
,isc.A.updateSize=function(_1){var _2=this.getDataElement();if(!_2)return;var _3,_4;var _5=this.getTextBoxHeight(),_6=this.$555(),_7=this.getScrollHeight(_1),_8=_2.offsetHeight;if((_7+_6)>_8){_2.style.height=_7+_6;_4=true}else if((_7+_6)<_8&&_8>_5){if((_7+_6)<_8){_3=true;_2.style.height=Math.max(_7+_6,_5)}
_4=true}
var _9=this.getTextBoxWidth(),_10=isc.Browser.isIE?0:this.$553(),_11=this.getScrollWidth(_1),_12=_2.offsetWidth;if((_11+_10)>_12){_2.style.width=(_11+_10);_4=true}else if((_11+_10)<_12&&_12>_9){_2.style.width=Math.max(_9,_11+_10);_3=true
_4=true}
if(_3)this.containerWidget.$t5=true;if(_4)this.adjustOverflow("Updated size to fit content")}
,isc.A.handleChanged=function(){this.updateSize();return this.Super("handleChanged",arguments)}
,isc.A.drawn=function(){this.Super("drawn",arguments);delete this.$554;delete this.$556;this.updateSize(true)}
,isc.A.redrawn=function(){this.Super("redrawn",arguments);delete this.$554;delete this.$556;this.updateSize(true)}
);isc.B._maxIndex=isc.C+10;isc.ClassFactory.defineClass("TimeItem","TextItem");isc.A=isc.TimeItem;isc.A.DEFAULT_TIME="00:00:00";isc.A=isc.TimeItem.getPrototype();isc.A.changeOnBlur=true;isc.A.changeOnKeypress=false;isc.A.width=100;isc.A.displayFormat="toShort24HourTime";isc.A.allowEmptyValue=true;isc.A=isc.TimeItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.short24TimeFormat="HH:MM";isc.A.shortTimeFormat="HH:MM [am|pm]";isc.A.long24TimeFormat="HH:MM:SS";isc.A.longTimeFormat="HH:MM:SS [am|pm]";isc.A.formatterMap={toTime:{mask:"[0-1]#:[0-6]#:[0-6]# [ap]m",formatter:"toPaddedTime"},to24HourTime:{mask:"[0-2]#:[0-6]#:[0-6]#",formatter:"toPadded24HourTime"},toPaddedTime:{mask:"[0-1]#:[0-6]#:[0-6]# [ap]m"},toPadded24HourTime:{mask:"[0-2]#:[0-6]#:[0-6]#"},toShortTime:{mask:"[0-1]#:[0-6]# [ap]m",formatter:"toShortPaddedTime"},toShort24HourTime:{mask:"[0-2]#:[0-6]#",formatter:"toShortPadded24HourTime"},toShortPaddedTime:{mask:"[0-1]#:[0-6]# [ap]m"},toShortPadded24HourTime:{mask:"[0-2]#:[0-6]#"}};isc.B.push(isc.A.getHint=function(){if(!this.showHint)return"";if(this.hint!=null)return this.hint;var _1=this.displayFormat;switch(_1){case"to24HourTime":case"toPadded24HourTime":return this.long24TimeFormat;case"toTime":case"toPaddedTime":return this.longTimeFormat;case"toShort24HourTime":case"toShortPadded24HourTime":return this.short24TimeFormat;case"toShortTime":case"toShortPaddedTime":return this.shortTimeFormat}
return""}
,isc.A.getDefaultValue=function(){var _1=this.defaultValue;if(!_1&&!this.allowEmptyValue)_1=isc.TimeItem.DEFAULT_TIME;if(_1&&!isc.isA.Date(_1))
_1=isc.Time.parseInput(_1);return _1}
,isc.A.mapValueToDisplay=function(_1){if(this.allowEmptyValue&&_1==null)return isc.emptyString;if(!isc.isA.Date(_1))_1=isc.Time.parseInput(_1);var _2=isc.Time.format(_1,this.displayFormat);_2=this.Super("mapValueToDisplay",_2);return _2}
,isc.A.mapDisplayToValue=function(_1){var _2=this.Super("mapDisplayToValue",arguments);if((_2==null||isc.isAn.emptyString(_2))&&this.allowEmptyValue){_2=null}else{_2=isc.Time.parseInput(_2)}
return _2}
,isc.A.updateValue=function(){this.Super("updateValue",arguments);if(!this.mask){this.setElementValue(this.mapValueToDisplay(this.getValue()))}}
,isc.A.setValue=function(_1){if(isc.isA.String(_1))_1=isc.Time.parseInput(_1);return this.Super("setValue",[_1])}
,isc.A.compareValues=function(_1,_2){if(_1==_2)return true;if(isc.isA.Date(_1)&&isc.isA.Date(_2)&&isc.Time.compareTimes(_1,_2))return true;return false}
,isc.A.init=function(){if(this.useMask){var _1=this.formatterMap[this.displayFormat];if(!_1){this.useMask=false;this.mask=null;this.logWarn("Mask will not be used because displayFormat "+this.displayFormat+" is not recognized")}else{if(_1.formatter)this.displayFormat=_1.formatter;this.mask=_1.mask}
if(this.mask){this.maskSaveLiterals=true;this.maskOverwriteMode=true}}else if(this.mask){this.mask=null}
this.Super("init",arguments)}
,isc.A.setMask=function(_1){this.logWarn("setMask: custom mask ignored")}
);isc.B._maxIndex=isc.C+9;isc.ClassFactory.defineClass("ToolbarItem","CanvasItem");isc.A=isc.ToolbarItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.canFocus=true;isc.A.shouldSaveValue=false;isc.A.showTitle=false;isc.A.buttonSpace=4;isc.A.startRow=true;isc.A.endRow=true;isc.A.colSpan="*";isc.A.canvasConstructor=isc.Toolbar;isc.A.vertical=false;isc.A.buttonConstructor=isc.AutoFitButton;isc.B.push(isc.A.isEditable=function(){return false}
);isc.B._maxIndex=isc.C+1;isc.A=isc.ToolbarItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.$18y=function(){var _1=(this.buttons||this.items||[]),_2=this.buttonProperties||{};isc.addProperties(_2,{handleActivate:function(){var _3=this.parentElement.canvasItem,_4=_3.form;if(this.click!=null)return this.click(_4,_3)}});if(this.buttonBaseStyle&&!_2.baseStyle){_2.baseStyle=this.buttonBaseStyle}
if(this.buttonTitleStyle&&!_2.titleStyle){_2.titleStyle=this.buttonTitleStyle}
this.$20r(_1);this.canvas={overflow:isc.Canvas.VISIBLE,buttons:_1,membersMargin:this.buttonSpace,vertical:this.vertical,buttonProperties:_2};var _5=this.height,_6=this.width;if(!isc.isA.Number(_6)&&this.$8t)
_6=isc.isA.Number(this.$8t[0])?this.$8t[0]:null;if(!isc.isA.Number(_5)&&this.$8t)
_5=isc.isA.Number(this.$8t[1])?this.$8t[1]:null;if(_5)this.canvas.height=_5;if(_6)this.canvas.width=_6;if(this.buttonConstructor!=null)
this.canvas.buttonConstructor=this.buttonConstructor;return this.Super("$18y",arguments)}
,isc.A.$20r=function(_1){if(!_1||_1.length==0)return;for(var i=0;i<_1.length;i++){if(_1[i].click&&isc.isA.String(_1[i].click)){_1[i].click=isc.Func.expressionToFunction("form,item",_1[i].click)}}}
,isc.A.getButton=function(_1){return isc.Class.getArrayItem(_1,this.buttons,"name")}
,isc.A.addButton=function(_1,_2){this.buttons=this.buttons||[];this.buttons.addAt(_1,_2);this.setButtons(this.buttons)}
,isc.A.removeButton=function(_1){var _2=isc.Class.getArrayItemIndex(_1,this.buttons,"name");if(_2!=-1){this.buttons.removeAt(_2);this.setButtons(this.buttons)}}
,isc.A.setButtons=function(_1){this.$20r(_1);this.buttons=_1;if(!this.canvas)return;this.canvas.setButtons(_1)}
,isc.A.setItems=function(_1){return this.setButtons(_1)}
);isc.B._maxIndex=isc.C+7;isc.ClassFactory.defineClass("UploadItem","TextItem");isc.A=isc.UploadItem.getPrototype();isc.A.$183="FILE";isc.A.$16b={onchange:isc.FormItem.$12y};isc.A=isc.UploadItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.elementHeight=(isc.Browser.isMoz?18:null);isc.B.push(isc.A.getTextBoxWidth=function(){if(isc.Browser.isMoz)return null;return this.Super("getTextBoxWidth",arguments)}
,isc.A.getTextBoxHeight=function(){if(this.elementHeight)return this.elementHeight;return this.Super("getTextBoxHeight",arguments)}
,isc.A.$149=function(){var _1=this.getElementWidth();if(!isc.isA.Number(_1))_1=185;return _1}
,isc.A.$10y=function(_1){_1=this.mapDisplayToValue(_1);if(_1==this._value)return true;var _2=this.handleChange(_1,this._value);if(this.$17n!=_1){this.logWarn("Upload Items do not support programatically modifying the value entered "+"by the user. Ignoring attempt to update from change handler")}
this.saveValue(this.mapDisplayToValue(this.getElementValue()));return _2}
,isc.A.redrawn=function(){this.Super("redrawn",arguments);this.updateValue(this.getElementValue())}
,isc.A.setValue=function(_1){var _2=this.getValue();if(_1==null||isc.isAn.emptyString(_1)){if(_2==null||isc.isAn.emptyString(_2))return;return this.Super("setValue",arguments)}
if(_1==_2){this.logInfo("Attempting to set the value for an upload form item to:"+_1+" This is the current value for the item so no action to take, but setting "+"UploadItems to a new value is always disabled.");return}
this.logWarn("Attempting to set the value for an upload form item. This is disallowed "+"for security reasons - returning the current value of the form item")}
,isc.A.setElementValue=function(_1){if(_1==null||isc.isAn.emptyString(_1)){if(isc.Browser.isIE){this.redraw();return}
return this.Super("setElementValue",arguments)}
this.logInfo("Attempting to set the value for an upload form item. This is disallowed "+"for security reasons - returning the current value of the form item");return this.getElementValue()}
);isc.B._maxIndex=isc.C+7;if(isc.Browser.isSafari){isc.A=isc.UploadItem.getPrototype();isc.A.colSpan="*";isc.A.startRow=true;isc.A.endRow=true;isc.A.width=300}
isc.defineClass("ComboBoxItem","TextItem","PickList");isc.A=isc.ComboBoxItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.autoSizePickList=true;isc.A.showPickerIcon=true;isc.A.pickerIconWidth=15;isc.A.pickerIconSrc="[SKIN]/DynamicForm/ComboBoxItem_PickButton_icon.gif";isc.A.pickerIconProperties={tabIndex:-1,showOver:true};isc.A.modalPickList=false;isc.A.showPickListOnKeypress=true;isc.A.$20s="Arrow_Up";isc.A.$20t="Arrow_Down";isc.A.$20u="Page_Up";isc.A.$20v="Page_Down";isc.A.$20w="Escape";isc.A.$10j="Enter";isc.A.$jf="Tab";isc.A.filterDisplayValue=true;isc.A.autoFetchData=false;isc.B.push(isc.A.drawn=function(_1,_2,_3,_4){this.invokeSuper(isc.ComboBoxItem,"drawn",_1,_2,_3,_4);if(this.autoFetchData&&this.$19m()){this.filterWithValue=false;this.fetchData(null,null,true)}}
,isc.A.pickListShown=function(){if(this.pickList.isVisible()){this.$20x=this.ns.Page.setEvent("mouseDown",this,null,"$20y")}}
,isc.A.$20y=function(){var _1=this.pickList;if(!_1||!_1.isVisible())return;var _2=isc.EH.lastEvent.target;if(!_1.contains(_2,true))_1.hide();if(!this.$43l){this.$43l=this.ns.Page.setEvent("mouseUp",this,isc.Page.FIRE_ONCE,"$20z")}
if(!this.$43m){this.$43m=this.ns.Page.setEvent("dragStop",this,isc.Page.FIRE_ONCE,"$43n")}}
,isc.A.$20z=function(){if(this.form.$ne().item==this){this.ns.Page.clearEvent("dragStop",this.$43m);delete this.$43m;delete this.$43l;return false}
this.$43n(true)}
,isc.A.$43n=function(_1){if(_1==true){this.ns.Page.clearEvent("dragStop",this.$43m)}else{this.ns.Page.clearEvent("mouseUp",this.$43l)}
delete this.$43m;delete this.$43l;if(this.pickList&&this.pickList.isVisible()&&this.pickList.contains(isc.EH.getTarget()))
{this.focusInItem()}}
,isc.A.pickListHidden=function(){if(this.$20x)this.ns.Page.clearEvent("mouseDown",this.$20x);delete this.$20x}
,isc.A.handleKeyPress=function(){if(!this.hasFocus)return this.Super("handleKeyPress",arguments);var _1=isc.EH.lastEvent.keyName,_2=this.pickList,_3=(_2?(_2.isDrawn()&&_2.isVisible()):false);if(_3&&(_1==this.$20v||_1==this.$20u)){return _2.body.handleKeyPress(isc.EH.lastEvent)}
var _4=this.getValue(),_5=(!_4||_4==isc.emptyString);if(_1==this.$20t&&isc.EH.altKeyDown()){if(_5)this.$200=true;this.filterWithValue=false;this.showPickList();return false}
if(_3){if(_1==this.$20t||_1==this.$20s||_1==this.$10j||_1==this.$20w)
{_2.bodyKeyPress(isc.EH.lastEvent);return false}}
return this.Super("handleKeyPress",arguments)}
,isc.A.handleKeyDown=function(){if(!this.hasFocus)return this.Super("handleKeyDown",arguments);var _1=isc.EH.lastEvent.keyName,_2=this.pickList,_3=(_2?(_2.isDrawn()&&_2.isVisible()):false);if(_3){if(_2&&_2.isVisible()&&_1==this.$jf&&(this.completeOnTab||this.$17h()==this.$14l))
{var _4=_2.getSelectedRecord();if(_4!=null)_2.itemClick(_4)}}
return this.Super("handleKeyDown",arguments)}
,isc.A.handleChange=function(_1,_2){var _3=this.Super("handleChange",arguments);if(_3==false)return false;_1=this.$17n;var _4=(!_1||_1==isc.emptyString);if(!_4)delete this.$200;var _5=this.pickList,_6=(_5?_5.isVisible():false);if(_4&&!this.$200){if(_6)_5.hide()}else if(this.showPickListOnKeypress||_6){this.saveValue(_1);if(!this.$201&&this.hasFocus){this.filterWithValue=true;this.showPickList(true,true)}}}
,isc.A.selectDefaultItem=function(){if(this.pickList==null||this.pickList.destroyed)return;var _1=this.pickList.selection;if(this.pickList.selection.anySelected())return;var _2=this.pickList.getRecord(0);if(_2==null||Array.isLoading(_2)||_2[this.pickList.isSeparatorProperty])return;_1.selectSingle(_2);this.pickList.clearLastHilite();this.pickList.scrollRecordIntoView(0)}
,isc.A.getPickListFilterCriteria=function(){var _1=this.optionCriteria||{};if(this.alwaysFilterWithValue||this.filterWithValue){var _2=this.getDisplayValue(),_3=(this.getDisplayFieldName()||this.getValueFieldName());_1[_3]=_2}
if(this.pickListCriteria)isc.addProperties(_1,this.pickListCriteria);return _1}
,isc.A.elementBlur=function(){this.Super("elementBlur",arguments);var _1=this.pickList,_2=isc.EH,_3=_2.lastEvent;if((_1&&((_3.eventType=="selectionChange"&&_1.contains(isc.EH.mouseDownTarget()))||((_3.eventType==_2.MOUSE_DOWN||_3.eventType==_2.CLICK)&&_1.contains(_3.target,true))))||(isc.Browser.isIE&&this.getActiveElement()==this.getDataElement()))
{return}
delete this.$19i;delete this.$43i;if(!_1||!_1.isVisible())return;_1.hide()}
,isc.A.editorEnter=function(_1,_2,_3){this.$66s()}
,isc.A.editorExit=function(_1,_2,_3){var _4;if(this.showHintInField&&(_3===_4||_3==null||isc.is.emptyString(_3)))
{this.$66t()}}
,isc.A.showPicker=function(){this.focusInItem();this.filterWithValue=false;return this.showPickList()}
,isc.A.pickValue=function(_1){var _2=this.mapValueToDisplay(_1);this.setElementValue(_2);this.$201=true;this.$66x={};this.$66x[_2]=_1;this.updateValue();delete this.$201}
,isc.A.setValueMap=function(){this.Super("setValueMap",arguments);if(this.pickList){if(this.pickList.isVisible())this.pickList.hide();delete this.pickList.formItem
this.setUpPickList(this.pickList.isVisible())}
this.setElementValue(this.mapValueToDisplay(this.getValue()))}
,isc.A.setValue=function(_1,_2,_3,_4,_5){this.$43o=true;this.invokeSuper(isc.ComboBoxItem,"setValue",_1,_2,_3,_4,_5);delete this.$43o;if(!this.hasFocus&&this.showHint&&this.showHintInField&&this.getHint()){var _6;if(_1===_6||_1==null||isc.is.emptyString(_1)){this.$66t()}}}
,isc.A.getDefaultValue=function(){var _1=this.Super("getDefaultValue",arguments);if(_1==null&&this.defaultToFirstOption){_1=this.getFirstOptionValue()}
return _1}
,isc.A.getCriteriaFieldName=function(){if(this.displayField&&!this.getSelectedRecord())return this.displayField;return this.getFieldName()}
,isc.A.$43f=function(_1,_2){var _3=(this.$17b(_1,true)!=null);if(_3)return;if(this.$43j){this.$43k=true;return}
this.invokeSuper(isc.ComboBoxItem,"$43f",_1)}
,isc.A.$17b=function(_1,_2,_3,_4,_5,_6){var _7=this.invokeSuper(isc.ComboBoxItem,"$17b",_1,true,_3,_4,_5,_6);if(_7==null&&this.getDisplayFieldName()!=null)
_7=this.$19o(_1,false);if(_7==null&&!_2)_7=_1;return _7}
,isc.A.mapDisplayToValue=function(_1,_2,_3,_4){if(this.$66x){for(var i in this.$66x){if(i==_1)return this.$66x[i]}
delete this.$66x}
if(this.getDisplayFieldName()!=null){var _6;_6=this.$19o(_1,true);if(_6!=null)_1=_6}
return this.invokeSuper(isc.ComboBoxItem,"mapDisplayToValue",_1,_2,_3,_4)}
);isc.B._maxIndex=isc.C+23;isc.ComboBoxItem.registerStringMethods({dataArrived:"startRow,endRow,data",getPickListFilterCriteria:""});isc.ClassFactory.defineClass("FileItem","CanvasItem");isc.A=isc.FileItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.shouldSaveValue=true;isc.B.push(isc.A.$18y=function(){this.canvas=isc.DynamicForm.create({autoDraw:false,_redrawWithParent:false,redrawOnResize:false,canSubmit:true,action:this.action,targetItem:this,getSaveOperationType:function(){if(this.targetItem&&this.targetItem.form)
return this.targetItem.form.getSaveOperationType();return this.Super("getSaveOperationType",arguments)},items:[{targetItem:this,type:"upload",width:this.width,height:this.height,name:this.getFieldName(),showTitle:false,saveValue:function(_1,_2,_3,_4){this.Super("saveValue",arguments);this.targetItem.saveValue(_1,_2,_3,_4)}},{name:"_transaction",type:"HiddenItem"}]});this.containerWidget.addChild(this.canvas)}
,isc.A.getValue=function(){return this.canvas.getValue(this.getFieldName())}
,isc.A.setValue=function(_1){if(_1==null||isc.isA.emptyString(_1)){this.canvas.setValue(this.getFieldName(),_1);return this.Super("setValue",arguments)}else{this.logWarn("Cannot programatically set the value of an upload field due to security restraints");return}}
,isc.A.setWidth=function(_1){if(this.canvas){this.canvas.items[0].setWidth(_1)}
this.Super("setWidth",arguments)}
,isc.A.setHeight=function(_1){if(this.canvas){this.canvas.items[0].setHeight(_1)}
this.Super("setHeight",arguments)}
);isc.B._maxIndex=isc.C+5;if(isc.ListGrid){isc.ClassFactory.defineClass("RelationItem","CanvasItem");isc.A=isc.RelationItem.getPrototype();isc.A.canvasConstructor="ListGrid";isc.A.canvasDefaults={canEdit:true};isc.A.pickerConstructor="RelationPicker";isc.A.showEditButton=true;isc.A.editButtonDefaults={click:"item.showPicker(!form.saveOperationIsAdd(), icon)",prompt:"Edit new/selected item"};isc.A.showRemoveButton=true;isc.A.removeButtonDefaults={src:"[SKIN]DynamicForm/Remove_icon.gif",click:"item.removeSelectedData()",prompt:"Remove selected item"};isc.A.canEditWithNoMasterRecord=false;isc.A=isc.RelationItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.disabled=!this.canEditWithNoMasterRecord;this.Super("init",arguments);if(!this.pickerDefaults)this.pickerDefaults={};isc.addProperties(this.pickerDefaults,{dataSource:this.dataSource});if(this.showEditButton)this.editButton=this.addIcon(this.editButtonDefaults);if(this.showRemoveButton)this.removeButton=this.addIcon(this.removeButtonDefaults)}
,isc.A.getPickerData=function(){var _1=this.canvas.getSelectedRecord();if(_1)return _1;return this.getDataSource().getForeignKeysByRelation(this.form.getValues(),this.form.dataSource)}
,isc.A.showPicker=function(_1,_2,_3,_4){this.Super("showPicker",arguments);var _5={};if(!this.form.saveOperationIsAdd())
_5=this.getDataSource().getForeignKeysByRelation(this.form.getValues(),this.form.dataSource);this.picker.setForeignKeyValues(_5)}
,isc.A.getValue=function(){return}
,isc.A.removeSelectedData=function(){this.canvas.removeSelectedData()}
,isc.A.setValue=function(){this.delayCall("filterRelation")}
,isc.A.filterRelation=function(){var _1=this.form.getValues();if(this.form.saveOperationIsAdd()){this.canvas.setData([]);this.setDisabled(!this.canEditWithNoMasterRecord)}else{this.canvas.filterData(this.getDataSource().getForeignKeysByRelation(_1,this.form.dataSource));this.enable()}
if(this.picker)this.picker.clearData()}
);isc.B._maxIndex=isc.C+7;isc.defineClass("RelationPicker","VLayout");isc.A=isc.RelationPicker.getPrototype();isc.A.className="dialogBackground";isc.A=isc.RelationPicker.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.creatorName="picker";isc.B.push(isc.A.initWidget=function(){this.Super("initWidget",arguments);this.addAutoChild("editor",{dataSource:this.dataSource},"DynamicForm");this.addAutoChild("toolbar",{membersMargin:2},"HLayout");this.addAutoChild("saveButton",{title:"Save",click:"this.picker.editor.saveData(this.picker.getID()+'.hide()')"},"AutoFitButton",this.toolbar);this.addAutoChild("clearButton",{title:"Clear",click:"this.picker.clearData();"},"AutoFitButton",this.toolbar);this.addAutoChild("cancelButton",{title:"Cancel",click:"this.picker.hide();this.picker.clearData()"},"AutoFitButton",this.toolbar)}
,isc.A.hide=function(){this.Super("hide",arguments);this.hideClickMask()}
,isc.A.setData=function(_1){this.editor.setData(_1)}
,isc.A.getData=function(){return this.editor.getValues()}
,isc.A.clearData=function(){this.editor.clearValues();this.setData(this.foreignKeyValues)}
,isc.A.dataChanged=function(){}
,isc.A.setForeignKeyValues=function(_1){this.foreignKeyValues=_1}
);isc.B._maxIndex=isc.C+7;}
if(isc.ListGrid){isc.ClassFactory.defineClass("MultiFileItem","RelationItem");isc.A=isc.MultiFileItem.getPrototype();isc.A.pickerConstructor="MultiFilePicker";isc.A.canvasDefaults={displayShortName:true,showHeader:false,emptyMessage:"Click icon to add...",canHover:true,cellHoverHTML:function(_1,_2,_3){if(this.canvasItem.form.saveOperationIsAdd())
return _1[this.getFieldName(_3)]},getCellValue:function(_1,_2,_3){var _1=this.Super("getCellValue",arguments);if(this.canvasItem.form.saveOperationIsAdd()){if(!this.displayShortName||!_1)return _1;_1=_1.replace(/.*(\\|\/)/g,isc.emptyString)}
return _1}};isc.A.iconWidth=16;isc.A.iconHeight=16;isc.A.editButtonDefaults=isc.addProperties({},isc.RelationItem.getInstanceProperty('editButtonDefaults'),{prompt:"Add files",src:"[SKIN]MultiFileItem/icon_add_files.png",showOver:false});isc.A.removeButtonDefaults=isc.addProperties({},isc.RelationItem.getInstanceProperty('removeButtonDefaults'),{src:"[SKIN]MultiFileItem/icon_remove_files.png",showOver:false,prompt:"Remove selected files"});isc.A.canEditWithNoMasterRecord=true;isc.A=isc.MultiFileItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.removeSelectedData=function(){if(!this.form.saveOperationIsAdd())return this.Super("removeSelectedData",arguments);var _1=this.canvas.getSelection();for(var i=0;i<_1.length;i++)this.picker.removeUploadField(_1[i]._form,true)}
,isc.A.formSaved=function(_1,_2,_3){if(this.picker){this.showPicker(true,this.editButton);this.picker.setForeignKeyValues(this.getDataSource().getForeignKeysByRelation(_3,this.form.dataSource));this.picker.saveData(this.getID()+".saveDataCallback()");return false}else{this.saveDataCallback()}}
,isc.A.saveDataCallback=function(){if(this.picker)this.picker.hide();this.form.formSavedComplete()}
,isc.A.pickerDataChanged=function(_1){if(!this.form.saveOperationIsAdd())return;this.canvas.setData(this.picker.getData())}
,isc.A.destroy=function(){this.Super("destroy");if(this.picker)this.picker.destroy()}
);isc.B._maxIndex=isc.C+5;isc.defineClass("MultiFilePicker","VStack");isc.A=isc.MultiFilePicker.getPrototype();isc.A.height=1;isc.A.layoutMargin=10;isc.A.styleName="dialogBackground";isc.A.minUploadFields=1;isc.A.minFileSizeForProgressBar=204800;isc.A.progressCheckFrequency=1000;isc.A.progressMeterConstructor="MultiFileProgressMeter";isc.A.uploadLayoutConstructor="VStack";isc.A.uploadWithPKButtonName="Save";isc.A.uploadWithoutPKButtonName="OK";isc.A.cancelButtonName="Cancel";isc.A.showUploadRemoveButton=true;isc.A.uploadWithoutPK=false;isc.A=isc.MultiFilePicker.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.creatorName="picker";isc.B.push(isc.A.initWidget=function(){this.Super("initWidget",arguments);this.addAutoChild("uploadLayout",{height:1});this.addAutoChild("addAnotherFileButton",{width:75,height:20,align:"left",cursor:isc.Canvas.HAND,icon:"[SKIN]MultiFileItem/icon_add_files.png",contents:"<u>Add&nbsp;another</u>",click:"this.picker.addUploadField()"},"Label");this.addAutoChild("toolbar",{width:1,height:1,membersMargin:10,layoutMargin:10,layoutAlign:"right"},"HStack");this.addAutoChild("saveButton",{title:this.hasKeys()?this.uploadWithPKButtonName:this.uploadWithoutPKButtonName,width:80,updateTitle:function(){var _1=this.parentElement.picker;var _2=_1.hasKeys()?_1.uploadWithPKButtonName:_1.uploadWithoutPKButtonName;if(_2!=this.title)this.setTitle(_2)},click:function(){var _1=this.parentElement.picker;if(!_1.hasKeys()&&!_1.uploadWithoutPK){_1.hide();_1.dataChanged()}else{_1.saveData()}},observes:[{source:this,message:"setForeignKeyValues",action:"observer.updateTitle()"}]},"IButton",this.toolbar);this.addAutoChild("cancelButton",{title:this.cancelButtonName,width:80,click:function(){this.picker.hide();if(this.picker.creator.form.saveOperationIsAdd())this.picker.clearData()}},"IButton",this.toolbar);this.clearData()}
,isc.A.hasKeys=function(){return(this.foreignKeyValues&&!isc.isAn.emptyObject(this.foreignKeyValues))}
,isc.A.setForeignKeyValues=function(_1){this.foreignKeyValues=_1}
,isc.A.clearData=function(){var _1=this.getForms();for(var i=0;i<_1.length;i++)this.removeUploadField(_1[i]);for(var i=0;i<this.minUploadFields;i++)this.addUploadField()}
,isc.A.addUploadField=function(){var _1=this.createAutoChild("uploadForm",{dataSource:this.dataSource,cellPadding:0,numCols:1,colWidths:['*'],width:250,elementChanged:function(){this.Super("elementChanged",arguments);this.picker.dataChanged()}},isc.DynamicForm);var _2=this.createAutoChild("uploadFormLayout",{members:[_1],height:21},isc.HLayout);this.uploadLayout.addMember(_2);if(this.showUploadRemoveButton){var _3=this.createAutoChild("uploadRemoveButton",{form:_1,picker:this,contents:isc.emptyString,cursor:isc.Canvas.HAND,icon:"[SKIN]MultiFileItem/icon_remove_files.png",click:"this.picker.removeUploadField(this.form, true)",iconSpacing:6,width:22,height:20},isc.Label);_2.addMember(_3,0)}
if(this.maxUploadFields&&this.maxUploadFields<=this.uploadLayout.getMembers().length)
this.addAnotherFileButton.hide()}
,isc.A.removeUploadField=function(_1,_2){if(_1.$203){isc.rpc.cancelQueue(_1.$203);this.transactionNum=null;if(this.progressMeter)this.progressMeter.hide()}
_1.parentElement.destroy();if(_2&&this.uploadLayout.getMembers().length<this.minUploadFields){this.addUploadField()}
if(this.maxUploadFields&&this.maxUploadFields>this.uploadLayout.getMembers().length){this.addAnotherFileButton.show()}
this.dataChanged()}
,isc.A.getForms=function(){return this.uploadLayout.getMembers().map("getMember",this.showUploadRemoveButton?1:0)}
,isc.A.dataChanged=function(){}
,isc.A.hide=function(){this.Super("hide",arguments);this.hideClickMask()}
,isc.A.getData=function(){var _1=[];var _2=this.getForms();for(var i=0;i<_2.length;i++){var _4=_2[i];var _5=_4.getValues();if(isc.isAn.emptyObject(_5))continue;_5._form=_4;_1[_1.length]=_5}
return _1}
,isc.A.saveData=function(_1){if(!_1)_1=this.saveCallback;if(!_1)_1=this.getID()+".hide()";this.saveCallback=_1;var _2=this.getForms();this.saveButton.setTitle(this.uploadWithPKButtonName);var _3;var _4;while(_2.length>0){_3=_2[0];_4=_3.getFields()[0].getValue();if(!_4){this.removeUploadField(_3);_2.remove(_3)}
else break}
if(_2.length==0){if(this.progressMeter){this.progressMeter.hide()}
this.transactionNum=null;delete this.saveCallback;this.fireCallback(_1);this.clearData();return}
if(!_3.validate())return;if(!this.progressMeter){this.progressMeter=this.createAutoChild("progressMeter",{progressCheckFrequency:this.progressCheckFrequency});this.addMember(this.progressMeter,0)}
_4=_4.replace(/.*(\\|\/)/g,isc.emptyString);this.progressMeter.setFileName(_4);this.progressMeter.hideProgressBar();this.progressMeter.show();if(this.hasKeys()){for(var _5 in this.foreignKeyValues)_3.setValue(_5,this.foreignKeyValues[_5])}
var _6=_3.saveData(this.getID()+".saveDataCallback(dsRequest, dsResponse, data)",{params:{formID:_3.getID(),singleUpload:"true"},willHandleError:true,form:_3,showPrompt:false,saveDataCallback:_1,timeout:0});this.transactionNum=_6.transactionNum;_3.$203=this.transactionNum;this.progressCheck(_3.getID(),this.transactionNum)}
,isc.A.saveDataCallback=function(_1,_2,_3){var _4=_1.form;if(_2.status!=isc.RPCResponse.STATUS_SUCCESS){this.progressMeter.hide();this.transactionNum=null;if(_2.status==isc.RPCResponse.STATUS_VALIDATION_ERROR){_4.setErrors(_2.errors,true)}else{isc.warn(_3)}
return}
_4.$203=null;this.removeUploadField(_4);this.saveData(_1.saveDataCallback);if(this.callingFormItem.fileUploaded){this.callingFormItem.fileUploaded(_1,_2)}}
,isc.A.progressCheck=function(_1,_2){this.lastProgressCheckTime=new Date().getTime();isc.DMI.callBuiltin({methodName:"uploadProgressCheck",callback:this.getID()+".progressCallback(rpcRequest, rpcResponse, data, "+_2+")",arguments:_1,requestParams:{willHandleError:true,showPrompt:false,formID:_1}})}
,isc.A.progressCallback=function(_1,_2,_3,_4){var _5=_1.formID;var _6=window[_5];if(!_6||this.transactionNum!==_4)return;if(_2.status!=isc.RPCResponse.STATUS_SUCCESS)this.progressCheck(_5);if(_3.errors){isc.rpc.cancelQueue(this.transactionNum);_6.setErrors(_3.errors,true);this.saveButton.show();this.transactionNum=null;this.progressMeter.hide();return}
this.progressMeter.setFileSize(_3.totalBytes);if(_3.totalBytes<this.minFileSizeForProgressBar){this.progressMeter.hideProgressBar();return}
this.progressMeter.setBytesReceived(_3.bytesSoFar);this.progressMeter.showProgressBar();this.progressMeter.setPercentDone(100*_3.bytesSoFar/ _3.totalBytes);var _7=this.progressCheckFrequency-(new Date().getTime()-this.lastProgressCheckTime);if(_7<0)_7=0;this.delayCall("progressCheck",[_5,_4],_7)}
);isc.B._maxIndex=isc.C+14;isc.defineClass("MultiFileProgressMeter","VStack");isc.A=isc.MultiFileProgressMeter;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.formatBytes=function(_1){var _2;if(_1<1024){_1=Math.round(_1/ 1024);_2="B"}else if(_1<(1024*1024)){_1=Math.round(_1/ 1024);_2="KB"}else{_1=Math.round(_1/(1024*1024)*100)/100;_2="MB"}
return _1+"&nbsp;"+_2}
);isc.B._maxIndex=isc.C+1;isc.A=isc.MultiFileProgressMeter.getPrototype();isc.A.height=50;isc.A=isc.MultiFileProgressMeter.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.initWidget=function(){this.Super("initWidget",arguments);this.addAutoChild("progressLabel",{height:1,dynamicContentsVars:{progressMeter:this},dynamicContents:true,contents:"<b><nobr>Saving ${progressMeter.fileName} ${progressMeter.getFormattedFileSize()}</nobr></b>"},"Canvas")}
,isc.A.setFileName=function(_1){this.fileName=_1;delete this.fileSize;this.bytesSoFar=0;this.bytesReceived=0;this.progressLabel.markForRedraw();if(this.progressBar)this.setPercentDone(0)}
,isc.A.setFileSize=function(_1){this.fileSize=_1;this.progressLabel.markForRedraw()}
,isc.A.setBytesReceived=function(_1){this.bytesSoFar=this.bytesReceived;;this.bytesReceived=_1;this.progressLabel.markForRedraw()}
,isc.A.getFormattedFileSize=function(){if(!this.fileSize)return isc.emptyString;var _1="<br>";if(this.bytesReceived){_1+=isc.MultiFileProgressMeter.formatBytes(this.bytesReceived)+" of "}
_1+=isc.MultiFileProgressMeter.formatBytes(this.fileSize);if(this.bytesSoFar&&this.progressCheckFrequency){var _2=this.bytesReceived-this.bytesSoFar;_2=isc.MultiFileProgressMeter.formatBytes(_2*1000/this.progressCheckFrequency);_1+=" ("+_2+"/sec)"}
return _1}
,isc.A.showProgressBar=function(){this.addAutoChild("progressBar",{overflow:"visible"},"Progressbar");this.progressBar.show()}
,isc.A.hideProgressBar=function(){if(this.progressBar)this.progressBar.hide()}
,isc.A.setPercentDone=function(_1){this.progressBar.setPercentDone(_1)}
);isc.B._maxIndex=isc.C+8;}
isc.addGlobal("MultiUploadItem",isc.MultiFileItem);isc.addGlobal("MultiUploadPicker",isc.MultiFilePicker);if(isc.ListGrid){isc.defineClass("DialogUploadItem","StaticTextItem");isc.A=isc.DialogUploadItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.iconHeight=16;isc.A.iconWidth=16;isc.A.icons=[{src:"[SKIN]MultiUploadItem/icon_add_files.png",prompt:"Upload File",click:"item.showPicker(true)"},{src:"[SKIN]MultiUploadItem/icon_remove_files.png",prompt:"Remove File",click:"item.removeFile()"}];isc.A.pickerConstructor="DialogUploadPicker";isc.A.noFileString="[NONE]";isc.B.push(isc.A.init=function(){this.Super("init",arguments);if(!this.pickerDefaults)this.pickerDefaults={};isc.addProperties(this.pickerDefaults,{dataSource:this.dataSource})}
,isc.A.mapValueToDisplay=function(_1){return _1==null?this.noFileString:this.Super("mapValueToDisplay",arguments)}
,isc.A.showPicker=function(){this.Super("showPicker",arguments);var _1=this.getValue('primaryKey');this.picker.foreignKeyValues={primaryKey:_1}}
,isc.A.removeFile=function(){var _1=this.getValue();if(_1!=this.defaultValue){var _2=isc.DataSource.get(this.dataSource);_2.removeData({primaryKey:_1},this.getID()+".removeFileCallback(dsResponse)")}}
,isc.A.removeFileCallback=function(_1){if(_1.status!=isc.DSResponse.STATUS_SUCCESS){isc.warn("Unable to remove file: "+_1.data);return}
this.setValue(this.defaultValue)}
,isc.A.fileUploaded=function(_1,_2){var _3=_2.data;var _4={};_4[_3.primaryKey]=_3.file_filename;this.setValueMap(_4);this.setValue(_3.primaryKey)}
,isc.A.destroy=function(){this.Super("destroy");if(this.picker)this.picker.destroy()}
);isc.B._maxIndex=isc.C+7;isc.defineClass("DialogUploadPicker","MultiFilePicker");isc.A=isc.DialogUploadPicker.getPrototype();isc.A.maxUploadFields=1;isc.A.uploadWithoutPKButtonName="Upload";isc.A.uploadWithPKButtonName="Upload";isc.A.showUploadRemoveButton=false;isc.A.uploadWithoutPK=true}
if(isc.ListGrid){isc.ClassFactory.defineClass("SOAPUploadItem","DialogUploadItem");isc.A=isc.SOAPUploadItem.getPrototype();isc.A.dataSource="sessionFiles"}isc.ClassFactory.defineClass("SpinnerItem","TextItem");isc.A=isc.SpinnerItem;isc.A.INCREASE_ICON={width:16,height:9,src:"[SKIN]/DynamicForm/Spinner_increase_icon.png",showOver:true,imgOnly:true,hspace:0};isc.A.DECREASE_ICON={width:16,height:9,src:"[SKIN]/DynamicForm/Spinner_decrease_icon.png",showOver:true,imgOnly:true,hspace:0};isc.A=isc.SpinnerItem.getPrototype();isc.A.changeOnKeypress=false;isc.A.canTabToIcons=false;isc.A.height=18;isc.A.step=1;isc.A=isc.SpinnerItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.Super("init",arguments);var _1=this.step;if(_1!=null&&!isc.isA.Number(_1)){_1=parseFloat(_1);if(!isc.isA.Number(_1))_1=1;this.step=_1}}
,isc.A.$14v=function(){if(this.icons==null)this.icons=[];var _1=isc.addProperties({},isc.SpinnerItem.INCREASE_ICON),_2=isc.addProperties({},isc.SpinnerItem.DECREASE_ICON);this.icons.addListAt([_1,_2],0);this.Super("$14v",arguments)}
,isc.A.getIconsHTML=function(){if(!this.showIcons)return"";var _1=isc.SB.create();if(!this.$204){var _2="<TD tabIndex=-1"+(isc.Browser.isIE?" style='font-size:0px'":"")+">",_3={},_4=this.$16l(_3),_5=this.$16m(_3);this.$204=["<TABLE STYLE='vertical-align:",_4,";margin-top:",_5,";margin-bottom:",_5,";display:inline;' BORDER=0 CELLPADDING=0 CELLSPACING=0><TR>",_2,this.getIconHTML(this.icons[0]),"<TD></TR><TR>",_2,this.getIconHTML(this.icons[1]),"</TD></TR></TABLE>"]}else{this.$204[8]=this.getIconHTML(this.icons[0]);this.$204[11]=this.getIconHTML(this.icons[1])}
_1.append(this.$204);for(var i=2;i<this.icons.length;i++){var _7=this.icons[i];if(!this.$16f(_7)||this.$16e(_7))continue;_1.append(this.getIconHTML(_7))}
return _1.toString()}
,isc.A.$16m=function(_1){if(_1==this.icons[0]||_1==this.icons[1])return 0;return this.Super("$16m",arguments)}
,isc.A.getTotalIconsWidth=function(){var _1=this.Super("getTotalIconsWidth",arguments);if(_1>0){var _2=Math.max(this.icons[0].width,this.icons[1].width);_1-=_2}
return _1}
,isc.A.mouseStillDown=function(_1,_2,_3){if(this.isDisabled())return;this.$205++;if(this.$18j)this.updateValue();var _4=_3.nativeTarget;if(_4==this.$16v(this.icons[0])){this.increaseValue()}else if(_4==this.$16v(this.icons[1])){this.decreaseValue()}}
,isc.A.mouseDown=function(_1,_2,_3){if(this.isDisabled())return;if(!this.hasFocus)this.focusInItem();this.$205=0}
,isc.A.handleKeyPress=function(_1,_2){if(this.Super("handleKeyPress",arguments)==false)return false;var _3=_1.keyName;if(_3=="Arrow_Up"){this.increaseValue();return false}
if(_3=="Arrow_Down"){this.decreaseValue();return false}}
,isc.A.increaseValue=function(){this.updateValue();var _1=this.getValue();if(_1!=null&&this.max==_1)return;var _2=this.$205;var _3=this.step*(_2!=null?Math.pow(2,Math.floor(this.$205/(2000/isc.EH.STILL_DOWN_DELAY))):1);return this.$206(_1,_3)}
,isc.A.decreaseValue=function(){this.updateValue();var _1=this.getValue();if(_1!=null&&this.min==_1)return;var _2=this.$205,_3=(0-this.step)*(_2!=null?Math.pow(2,Math.floor(this.$205/(2000/isc.EH.STILL_DOWN_DELAY))):1);return this.$206(_1,_3)}
,isc.A.$206=function(_1,_2){var _3=this.min,_4=this.max;if(!isc.isA.Number(_1)){_1=0;if((_3!=null&&_1<_3)||(_4!=null&&_1>_4)){_1=(_3!=null?_3:_4)}}else{var _5,_6;if(Math.round(_2)==_2){_5=0}else{var _7=_2+"";_5=_7.length-(_7.indexOf(".")+1)}
if(Math.round(_1)==_1){_6=0}else{var _8=_1+"";_6=_8.length-(_8.indexOf(".")+1)}
_1+=_2;var _9=Math.max(_5,_6);if(_9>0){_1=parseFloat(_1.toFixed(_9))}
if(_2>0&&_4!=null&&_1>_4)_1=_4;else if(_2<0&&_3!=null&&_1<_3)_1=_3}
this.setElementValue(_1);this.updateValue()}
,isc.A.mapDisplayToValue=function(_1){_1=this.Super("mapDisplayToValue",arguments);if(isc.isA.String(_1)){var _2=parseFloat(_1);if(_2==_1)_1=_2}
return _1}
,isc.A.updateValue=function(){var _1=this.getElementValue();_1=this.mapDisplayToValue(_1);if(_1==this._value)return;if(_1!=null&&(!isc.isA.Number(_1)||(this.max!=null&&_1>this.max)||(this.min!=null&&_1<this.min)))
{var _2=this.mapValueToDisplay(this._value);this.setElementValue(_2);return}
this.Super("updateValue",arguments)}
,isc.A.setValue=function(_1,_2,_3,_4,_5,_6){if(_1!=null&&!isc.isA.Number(_1)){var _7=parseFloat(_1);if(_7==_1)_1=_7;else{this.logWarn("setValue(): passed '"+_1+"'. This is not a valid number - rejecting this value");_1=null}}
if(_1!=null){if(this.max!=null&&_1>this.max){this.logWarn("setValue passed "+_1+" - exceeds specified maximum. Clamping to this.max.");_1=this.max}
if(this.min!=null&&_1<this.min){this.logWarn("setValue passed "+_1+" - less than specified minimum. Clamping to this.min.");_1=this.min}}
return this.invokeSuper(isc.SpinnerItem,"setValue",_1,_2,_3,_4,_5,_6)}
);isc.B._maxIndex=isc.C+14;isc.ClassFactory.defineClass("SliderItem","CanvasItem");isc.A=isc.SliderItem.getPrototype();isc.A.vertical=false;isc.A.minValue=1;isc.A.maxValue=100;isc.A.roundValues=true;isc.A.roundPrecision=1;isc.A.defaultValue=1;isc.A.shouldSaveValue=true;isc.A.sliderDefaults={autoDraw:false,showTitle:false,valueChanged:function(){if(this.canvasItem)this.canvasItem.sliderChange()}};isc.A.autoDestroy=true;isc.A=isc.SliderItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.changeOnDrag=true;isc.B.push(isc.A.init=function(){this.Super("init",arguments);this.$207=this.getDefaultValue()}
,isc.A.$18y=function(){var _1=this.sliderDefaults;var _2=isc.addProperties({},_1,{vertical:this.vertical,minValue:this.minValue,maxValue:this.maxValue,value:this.defaultValue,numValues:this.numValues,roundValues:this.roundValues,roundPrecision:this.roundPrecision,tabIndex:this.getGlobalTabIndex()},this.sliderProperties);this.canvas=isc.Slider.create(_2);this.Super("$18y",arguments)}
,isc.A.isEditable=function(){return true}
,isc.A.setValue=function(_1){this.$17g=true;var _2;if(_1==null){_2=this.getDefaultValue();if(_2!=null)_1=_2}
this.$207=_1;this.canvas.setValue(_1,(_2!=null))}
,isc.A.getValue=function(){return this.canvas.getValue()}
,isc.A.sliderChange=function(){var _1=this.canvas.getValue();if(this.$207!=_1){if(this.changeOnDrag||!this.canvas.valueIsChanging()){this.$10y(_1);this.$207=_1}}else{this.saveValue(_1)}}
,isc.A.setMinValue=function(_1){this.canvas.setMinValue(_1)}
,isc.A.setMaxValue=function(_1){this.canvas.setMaxValue(_1)}
,isc.A.setNumValues=function(_1){this.canvas.setNumValues(_1)}
);isc.B._maxIndex=isc.C+9;isc.ClassFactory.defineClass("ColorItem","TextItem");isc.A=isc.ColorItem.getPrototype();isc.A.changeOnBlur=true;isc.A.changeOnKeypress=false;isc.A.pickerConstructor="ColorPicker";isc.A.pickerDefaults={colorSelected:function(_1,_2){this.callingFormItem.pickerColorSelected(_1,_2)}};isc.A.showPickerIcon=true;isc.A.pickerIconWidth=18;isc.A.pickerIconHeight=18;isc.A.pickerIconSrc="[SKIN]/DynamicForm/ColorPicker_icon.png";isc.A.pickerIconProperties={prompt:"Click to select a new color",showOver:false};isc.A.defaultPickerMode="simple";isc.A.allowComplexMode=true;isc.A.supportsTransparency=false;isc.A.browserSpellCheck=false;isc.A=isc.ColorItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.pickerDefaults.defaultPickMode=this.defaultPickerMode;this.pickerDefaults.allowComplexMode=this.allowComplexMode;this.pickerDefaults.supportsTransparency=this.supportsTransparency;this.Super("init",arguments)}
,isc.A.updateValue=function(){var _1=this._value,_2=this.getElementValue();_2=this.mapDisplayToValue(_2);if(_2==this._value)return;if(_2!=null&&!isc.isA.color(_2)){this.setElementValue(_1);return}
this.Super("updateValue",arguments);if(this.showPickerIcon&&this._value!=_1){this.setIconBackgroundColor(this.getPickerIcon(),this._value)}}
,isc.A.getDefaultValue=function(){var _1=this.Super("getDefaultValue",arguments);if(_1&&!isc.isA.color(_1)){this.logWarn("Default value:"+_1+" is not a valid color identifier."+" Ignoring this default.");_1=this.defaultValue=null}
return _1}
,isc.A.showPicker=function(){if(!this.picker){this.picker=isc.ColorChooser.getSharedColorPicker(this.pickerDefaults)}
var _1=this.picker;var _2=_1.callingFormItem;if(_2!=this){_1.callingFormItem=this;_1.callingForm=this.form;_1.setSupportsTransparency(this.supportsTransparency)}
return this.Super("showPicker",arguments)}
,isc.A.pickerColorSelected=function(_1,_2){if(!this.mask){_1=this.mapValueToDisplay(_1)}
this.setElementValue(_1);this.updateValue()}
,isc.A.setValue=function(_1){this.Super("setValue",arguments);this.setIconBackgroundColor(this.getPickerIcon(),this._value)}
);isc.B._maxIndex=isc.C+6;if(isc.ListGrid){isc.ClassFactory.defineClass("PickTreeItem","CanvasItem");isc.A=isc.PickTreeItem;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.$21j=function(_1){return this.canvasItem.$21j(_1)}
,isc.A.$21k=function(){var _1=this.getSelectedItem();if(_1==null){var _2=this.canvasItem,_3=this.canvasItem.getValue();if(_3!=null)return _2.mapValueToDisplay(_3)}
return this.Super("getTitle",arguments)}
,isc.A.$21l=function(){var _1=this.canvasItem;_1.setValue(_1.getValue())}
);isc.B._maxIndex=isc.C+3;isc.A=isc.PickTreeItem.getPrototype();isc.A.canFocus=true;isc.A.shouldSaveValue=true;isc.A.buttonDefaults={height:19};isc.A.emptyMenuMessage="No items to display";isc.A=isc.PickTreeItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.buttonConstructor="TreeMenuButton";isc.B.push(isc.A.init=function(){this.Super("init",arguments);if(this.dataSource==null&&this.valueTree==null)
this.logWarn("This form item requires a 'valueTree'.")}
,isc.A.$18y=function(){var _1={getTitle:isc.PickTreeItem.$21k,canFocus:this.$kk(),disabled:this.isDisabled(),dataSource:this.dataSource,data:this.valueTree,canSelectParentItems:this.canSelectParentItems,itemSelected:isc.PickTreeItem.$21j,emptyMenuMessage:this.emptyMenuMessage,loadDataOnDemand:this.loadDataOnDemand,treeDataLoaded:isc.PickTreeItem.$21l,displayField:this.displayField};this.canvas=this.addAutoChild("button",_1,this.buttonConstructor,this.container);this.autoDestroy=true;this.Super("$18y",arguments)}
,isc.A.$21j=function(_1){var _2=this.$21m(_1);if(this.$10y(_2))this.$21n=_1}
,isc.A.$21m=function(_1){if(this.$21o())return this.valueTree.getPath(_1);return _1[this.$21p()]}
,isc.A.$21o=function(){return(!this.valueField&&this.valueTree&&(this.valueTree.modelType!="parent"))}
,isc.A.$21p=function(){var _1=this.valueField;if(!_1){_1=this.valueTree?this.valueTree.idField:this.$21q()}
return _1}
,isc.A.$21q=function(){if(!this.dataSource)return null;if(!this.$21r){var _1=isc.DataSource.getDataSource(this.dataSource),_2=_1.getPrimaryKeyFieldNames(),_3=isc.isAn.Array(_2)?_2[0]:_2;if(isc.isAn.Array(_2)&&_2.length>1){this.logWarn("Multiple primary key fields not supported by PickTreeItem - using '"+_3+"' as single primary key field")}
this.$21r=_3}
return this.$21r}
,isc.A.getSelectedNode=function(){return this.$21n}
,isc.A.setValue=function(_1){this.Super("setValue",arguments);var _2=this.$21s(this.getValue());this.$21n=_2;if(this.canvas){this.canvas.setSelectedItem(_2)}}
,isc.A.$21s=function(_1){if(!_1)return null;var _2=(this.dataSource?this.canvas.getTree():this.valueTree);if(this.$21o())return this.valueTree.find(_1);return _2.find(this.$21p(),_1)}
);isc.B._maxIndex=isc.C+10;isc.defineClass("IPickTreeItem","PickTreeItem");isc.A=isc.IPickTreeItem.getPrototype();isc.A.buttonConstructor="ITreeMenuButton"}
isc.ClassFactory.defineClass("PopUpTextAreaItem","StaticTextItem");isc.A=isc.PopUpTextAreaItem.getPrototype();isc.A.canFocus=true;isc.A.wrap=false;isc.A.width=150;isc.A.clipValue=true;isc.A.popUpOnEnter=false;isc.A.popUpOnAnyClick=true;isc.A.textAreaWidth=100;isc.A.textAreaHeight=100;isc.A.iconOnly=false;isc.A.popUpIconSrc="[SKIN]/DynamicForm/PopUpTextAreaEditor_icon.gif";isc.A.popUpIconWidth=20;isc.A.popUpIconHeight=20;isc.A.iconVAlign=isc.Canvas.CENTER;isc.A=isc.PopUpTextAreaItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.$14v=function(){if(this.icons==null)this.icons=[];var _1={src:this.popUpIconSrc,showOver:false,width:this.popUpIconWidth,height:this.popUpIconHeight,click:this.$43p};this.icons.addAt(_1,0);this.Super("$14v",arguments)}
,isc.A.$43p=function(_1,_2,_3){if(_2.popUpOnAnyClick||_2.isDisabled())return;_2.showPopUp(true)}
,isc.A.handleCellClick=function(){if(this.Super("handleCellClick")==false)return false;if(this.popUpOnAnyClick&&!this.isDisabled())this.showPopUp(true)}
,isc.A.showPopUp=function(_1){var _2=this.getValue();if(!this.$21t)this.setupPopUpForm();this.placePopUp();var _3=this.$21t.getItem("textArea");_3.setValue(_2);this.$21t.bringToFront();this.$21t.show();if(_1)this.$21t.focusInItem("textArea");this.$21t.showClickMask({target:this,methodName:"hidePopUp"},true,[this.$21t])}
,isc.A.visibilityChanged=function(){if(!this.isVisible())this.$21u()}
,isc.A.$21u=function(){var _1=this.$21t;if(!_1||!(_1.isVisible()&&_1.isDrawn()))return;_1.hide()}
,isc.A.moved=function(){this.$21v()}
,isc.A.$21v=function(){var _1=this.$21t;if(!_1||!(_1.isVisible()&&_1.isDrawn()))return;var _2=this.getTop(),_3=this.getLeft(),_4=this.getInnerWidth(),_5=this.getInnerHeight(),_6=this.containerWidget,_7=_6.getScrollTop(),_8=_6.getScrollLeft(),_9=_6.getViewportWidth(),_10=_6.getViewportHeight();if(_2<_7||(_2+_5)>(_7+_10)||_3<_8||(_3+_4)>(_8+_9))
{_1.hide()}else{this.placePopUp()}}
,isc.A.zIndexChanged=function(){var _1=this.$21t;if(!_1||!(_1.isVisible()&&_1.isDrawn()))return;_1.bringToFront()}
,isc.A.placePopUp=function(){var _1=this.getTextAreaTop(),_2=this.getTextAreaLeft(),_3=this.getTextAreaWidth(),_4=this.getTextAreaHeight();this.$21t.moveTo(_2,_1);this.$21t.resizeTo(_3,_4);var _5=this.$21t.getItem("textArea");_5.setWidth(_3);_5.setHeight(_4)}
,isc.A.setupPopUpForm=function(){if(this.$21t!=null)return;var _1=isc.DynamicForm.create({autoDraw:false,ID:this.getID()+"$21t",_generated:true,separateContentInsertion:false,cellPadding:0,targetItem:this,values:{textArea:this.getValue()},items:[{name:"textArea",showTitle:false,type:"textArea",selectOnFocus:true,targetItem:this,focus:function(_4){this.targetItem.textAreaFocus()},keyDown:function(_4,_5,_6,_7){this.targetItem.$18i();return this.targetItem.textAreaKeyDown(_4,_6,_7)},keyPress:function(_4,_5,_6,_7){return this.targetItem.textAreaKeyPress(_4,_6,_7)},blur:function(){this.targetItem.textAreaBlur()}}],hide:function(_4,_5,_6,_7){var _2=this.invokeSuper(isc.DynamicForm,"hide",_4,_5,_6,_7);this.hideClickMask();return _2}});this.$21t=_1;var _3=this.containerWidget;_1.observe(_3,"destroy","observer.hide();observer.destroy()")}
,isc.A.hidePopUp=function(){if(this.$21t){this.updateValue();this.$21t.hide()}}
,isc.A.destroy=function(){if(this.$21t){this.$21t.destroy();delete this.$21t}
return this.Super("destroy",arguments)}
,isc.A.getTextAreaTop=function(){var _1=this.getPageTop();if(isc.Browser.isIE)_1-=1;return _1}
,isc.A.getTextAreaLeft=function(){return this.getPageLeft()}
,isc.A.getTextAreaWidth=function(){return Math.max(this.textAreaWidth,this.getInnerWidth())}
,isc.A.getTextAreaHeight=function(){return this.textAreaHeight}
,isc.A.mapValueToDisplay=function(){if(this.iconOnly)return"";return this.Super("mapValueToDisplay",arguments)}
,isc.A.textAreaBlur=function(){this.hidePopUp()}
,isc.A.textAreaFocus=function(){}
,isc.A.textAreaKeyPress=function(_1,_2,_3){}
,isc.A.textAreaKeyDown=function(_1,_2,_3){}
,isc.A.setValue=function(_1){var _2=this.mapValueToDisplay(this.getValue());this.Super("setValue",arguments);var _3=this.mapValueToDisplay(this.getValue());if(_2!=_3){this.setElementValue(_3)
if(this.$21t&&this.$21t.isVisible()){this.$21t.setValue("textArea",_3)}}}
,isc.A.updateValue=function(){if(this.$21t&&this.$21t.isVisible()&&!this.$21t.$10u)
{var _1=this.$21t.getItem("textArea");_1.updateValue();var _2=this.$21t.getValue("textArea");this.setElementValue(this.mapValueToDisplay(_2));this.$10y(_2)}else{return this.Super("updateValue",arguments)}}
,isc.A.setElementValue=function(_1){if(this.iconOnly)return;return this.Super("setElementValue",arguments)}
,isc.A.focusInItem=function(){if(this.$21t&&this.$21t.isVisible()){this.$21t.focusInItem('textArea')}else if(this.showIcons){this.focusInIcon(this.icons[0])}else{this.showPopUp(true)}}
,isc.A.$173=function(_1){this.$174=_1;if(!this.isVisible()||!this.containerWidget.isDrawn())return;this.$175()}
);isc.B._maxIndex=isc.C+27;isc.defineClass("ExpressionItem","PopUpTextAreaItem");isc.A=isc.ExpressionItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.textAreaWidth=400;isc.A.showActionIcon=true;isc.A.actionIconSrc="[SKIN]/actions/add.png";isc.A.actionIconWidth=20;isc.A.actionIconHeight=20;isc.A.actionIconPosition=1;isc.B.push(isc.A.mapValueToDisplay=function(_1){if(isc.isA.StringMethod(_1))return _1.getDisplayValue();else if(isc.isA.Function(_1)){if(_1.iscAction){return"["+_1.iscAction.title+"]"}
return isc.Func.getBody(_1)}
else return this.Super("mapValueToDisplay",arguments)}
,isc.A.getValue=function(){var _1=this.Super("getValue");if(isc.isA.Function(_1))return isc.Func.getBody(_1);else return _1}
,isc.A.$14v=function(){this.Super("$14v",arguments);if(this.showActionIcon){if(this.icons==null)this.icons=[];var _1=this.actionIconPosition;this.icons.addAt({src:this.actionIconSrc,showOver:false,width:this.actionIconWidth,height:this.actionIconHeight,click:this.getID()+".showActionMenu();return false;"},_1);this.$36c(this.icons[_1])}}
,isc.A.updateAppearance=function(_1){this.setElementValue(this.mapValueToDisplay(_1))}
,isc.A.showActionMenu=function(){var _1=this,_2=isc.ActionMenu.create({sourceComponent:this.form.currentComponent,sourceMethod:this.name,components:this.form.allComponents,bindingComplete:function(_4){_1.$10y(_4)}});_2.show();var _3=this.getIconPageRect(this.icons[1]);_2.placeNear(_3[0]+this.actionIconWidth,_3[1]+this.actionIconHeight-this.containerWidget.getScrollTop())}
);isc.B._maxIndex=isc.C+5;isc.ClassFactory.defineClass("SearchForm","DynamicForm");isc.A=isc.SearchForm.getPrototype();isc.A.hiliteRequiredFields=false;isc.A.operationType="fetch";isc.A.$66m=true;isc.A=isc.SearchForm.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.$21w="DateItem";isc.B.push(isc.A.createItem=function(_1,_2,_3,_4,_5){var _6=this.getDataSource(),_7=_6?_6.getField(_1[this.fieldIdProperty])!=null:false;if(_7){var _8=isc.FormItemFactory.getItemClassName(_1,_2,this),_9=isc.FormItemFactory.getItemClass(_8);if(_9==isc.DateItem&&_1&&(_1.useTextField==null))
_1.useTextField=true;if(_1.allowEmptyValue==null)_1.allowEmptyValue=true}
return this.invokeSuper(isc.SearchForm,"createItem",_1,_2,_3,_4,_5)}
,isc.A.submitValues=function(_1,_2){if(this.search!=null){return this.search(this.getValuesAsCriteria(),this)}}
,isc.A.validate=function(_1,_2,_3){if(this.validateTypeOnly){return this.invokeSuper(isc.SearchForm,"validate",_1,_2,true)}else{return this.invokeSuper(isc.SearchForm,"validate",_1,_2,_3)}}
);isc.B._maxIndex=isc.C+3;isc.A=isc.SearchForm.getPrototype();isc.A.showFilterFieldsOnly=true;isc.A.validateTypeOnly=true;isc.SearchForm.registerStringMethods({search:"criteria,form"});isc.ClassFactory.defineClass("ValuesManager");isc.A=isc.ValuesManager.getPrototype();isc.A.unknownErrorMessage="Invalid value";isc.A=isc.ValuesManager.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.ns.ClassFactory.addGlobalID(this);if(this.dataSource)this.bindToDataSource(this.dataSource);this.values=isc.addProperties({},this.values);if(this.members!=null){var _1=this.members;this.members=null;if(!isc.isAn.Array(_1))_1=[_1];for(var i=0;i<_1.length;i++){this.addMember(_1[i])}}
this.rememberValues()}
,isc.A.destroy=function(){var _1=this.members;if(_1){for(var i=_1.length-1;i>=0;i--){this.removeMember(_1[i])}}
window[this.getID()]=null}
,isc.A.$71e=function(_1,_2,_3,_4){_2=(_2!=null)?this.$702(_1,_2):_1;return this.$10y(_2,_3,_4)}
,isc.A.$10y=function(_1,_2,_3){if(isc.isA.DynamicForm(_3)&&_3.getItem(_1)==null){this.$66q(_3,_1);return}
var _4;var _5=_3.getFullDataPath();if(_5){_1=(_1!=null)?this.$702(_5,_1):_5;_4=true}else _4=_1.contains(isc.Canvas.$70l);if(!_4){this.values[_1]=_2}else{isc.DynamicForm.$70n(_1,_2,this.values)}}
,isc.A.$702=function(_1,_2){return isc.DynamicForm.$702(_1,_2)}
,isc.A.$66q=function(_1,_2){this.logWarn("Member Form: "+_1+" has explicitly specified value for field[s] '"+_2+"', but has"+" no item associated with this fieldName. Ignoring this value. "+"Values may be set for fields with no associated form item directly "+"on the valuesManager via valuesManager.setValues(), but not on "+"member forms. See ValuesManager documentation for more info.")}
,isc.A.$10z=function(_1,_2){var _3=_2.getFullDataPath();if(_3)_1=this.$702(_3,_1);return isc.DynamicForm.$70m(_1,this.values)}
,isc.A.bindToDataSource=function(_1){if(!isc.isA.DataSource(_1))_1=isc.DataSource.getDataSource(_1);if(_1!=null)this.dataSource=_1}
,isc.A.setDataSource=function(_1,_2){this.bindToDataSource(_1)}
,isc.A.getDataSource=function(){if(isc.isA.String(this.dataSource)){if(this.serviceNamespace||this.serviceName){this.dataSource=this.lookupSchema()}else{var _1=isc.DS.get(this.dataSource);if(_1!=null)return _1;_1=this.getWindow()[this.dataSource];if(_1&&isc.isA.DataSource(_1))return(this.dataSource=_1)}}
return this.dataSource}
,isc.A.lookupSchema=function(){var _1;if(this.serviceName)_1=isc.WebService.getByName(this.serviceName,this.serviceNamespace);else _1=isc.WebService.get(this.serviceNamespace);if((this.serviceNamespace||this.serviceName)&&_1==null){this.logWarn("Could not find WebService definition: "+(this.serviceName?"serviceName: "+this.serviceName:"")+(this.serviceNamespace?"   serviceNamespace: "+this.serviceNamespace:"")+this.getStackTrace())}
if(!isc.isA.String(this.dataSource)){this.logWarn("this.dataSource was not a String in lookupSchema");return}
if(_1)return _1.getSchema(this.dataSource)}
,isc.A.getDataSourceField=function(_1){var _2=this.getDataSource();if(!_2||!_1)return null;_1=_1.trim("/");var _3=this.getDataSource(),_4=_1.split("/"),_5;for(var i=0;i<_4.length;i++){if(isc.isAn.emptyString(_4[i]))continue;var _7=_4[i];_5=_3.getField(_7);_3=_5?isc.DataSource.getDataSource(_5.type):_3}
return _5}
,isc.A.getItems=function(){if(!this.members)return;var _1=[];for(var i=0;i<this.members.length;i++){var _3=this.members[i];if(!_3.getItems)continue;_1.addList(_3.getItems())}
return _1}
,isc.A.getFields=function(){return this.getItems()}
,isc.A.getItem=function(_1){return this.$706(_1,true)}
,isc.A.getField=function(_1){return this.getItem(_1)}
,isc.A.getMembers=function(){return this.members}
,isc.A.getMember=function(_1){var _2=window[_1];if(this.members&&this.members.contains(_2))return _2;return null}
,isc.A.getMemberForField=function(_1,_2){return this.$706(_1,false,_2)}
,isc.A.$706=function(_1,_2,_3){if(!this.members||_1==null||isc.isAn.emptyString(_1))return null;_1=_1.trim("/");var _4=_1.split(isc.Canvas.$70l);var _5=_3?[]:null;for(var i=0;i<this.members.length;i++){var _7=this.members[i],_8=_7.getFullDataPath();if(_8==isc.Canvas.$70l||isc.isAn.emptyString(_8))
_8=null;if(_4&&_4.length>0&&_8!=null){var _9=null;for(var _10=0;_10<_4.length;_10++){_9=!_9?_4[_10]:(_9+isc.Canvas.$70l+_4[_10]);if(_8.endsWith(isc.Canvas.$70l)){_8=_8.substring(0,_8.length-1)}
if(_8==_9){if(!_2&&(_10==_4.length-1)){if(!_3)return _7;_5.add(_7);break}
if(_7.getField){var _11=_4.slice(_10+1).join(isc.Canvas.$70l);var _12=_7.getField(_11);if(_12){if(_2){if(!isc.isA.FormItem(_12))_12=null;if(_3){if(_12)_5.add(_12)}else{return _12}}else{if(_3)_5.add(_7);else return _7}}}}}}else{if(_1.startsWith(isc.Canvas.$70l))_1=_1.substring(1);if(this.members[i].getItem){var _13=this.members[i].getField(_1);if(_13){if(_2){if(!isc.isA.FormItem(_13))_13=null;if(_3){if(_13)_5.add(_13)}else{return _13}}else{if(_3)_5.add(_7);else return _7}}}}}
return _3?_5:null}
,isc.A.getFileItemForm=function(){if(!this.members)return;var _1=false,_2;for(var i=0;i<this.members.length;i++){if(this.members[i].getFileItemForm==null)continue;var _4=this.members[i].getFileItemForm();if(_4){if(_1){this.logWarn("ValuesManager defined with more than one member form "+" containing a FileItem. This is not supported - binary data may "+"only be uploaded from one FileItem when saving ValuesManager data")}else{_2=_4;_1=true}}}
return _2}
,isc.A.validate=function(){if(this.disableValidation)return true;if(this.dataSource&&this.dataSource.useLocalValidators!=null&&this.useLocalValidators==false)return true;this.clearHiddenErrors();var _1=true,_2=this.dataSource?isc.addProperties({},this.getDataSource().getFields()):null,_3={};if(this.members){for(var i=0;i<this.members.length;i++){if(!isc.isA.DynamicForm(this.members[i]))continue;var _5=this.members[i],_6=_5.disableValidation,_7=this.members[i].getItems();if(!_6){if(_5.handleHiddenValidationErrors!=null){this.logInfo("form level 'handleHiddenValidationErrors' method suppressed "+"in favor of valuesManager level handler","validation");_5.$43q=_5.handleHiddenValidationErrors}
_5.handleHiddenValidationErrors=this.$43r}
for(var j=0;j<_7.length;j++){var _9=_7[j].dataPath||_7[j].getFieldName();if(_2&&this.members[i].getDataSource()==this.getDataSource())
delete _2[_9]}
var _10=_6?true:_5.validate(true,true)
_1=(_1&&_10);if(!_6){if(_5.$43s)_5.handleHiddenValidationErrors=_5.$43s;else delete _5.handleHiddenValidationErrors}
if(!_10&&!(_5.isDrawn()&&_5.isVisible())){this.addHiddenErrors(_5.errors,_5)}}}
var _11=this.getValues(),_12={};for(var _9 in _2){var _13=_2[_9],_3=_13.validators,_14=_11[_9];if(_3!=null){var _14=_11[_9];for(var i=0;i<_3.length;i++){var _15=_3[i];if(!_15)continue;if(_14==null&&_15.type!='required'&&_15.type!="requiredIf")
{continue}
if(!this.processValidator(_13,_15,_14,null,_11)){if(_12[_9]==null)_12[_9]=[];var _16=_15.errorMessage||this.unknownErrorMessage;_12[_9].add(_16)}}}
if(_12[_9]&&_12[_9].length==1)_12[_9]=_12[_9][0]}
this.addHiddenErrors(_12);this.$21z(true);if(isc.getKeys(_12).length>0)_1=false;return _1}
,isc.A.getValidatedValues=function(){if(!this.validate())return null;return this.getValues()}
,isc.A.$43r=function(_1){var _2=this.valuesManager;_2.addHiddenErrors(_1,this);return false}
,isc.A.clearHiddenErrors=function(){delete this.hiddenErrors}
,isc.A.addHiddenErrors=function(_1,_2){if(_1==null||isc.isAn.emptyObject(_1))return;if(!this.hiddenErrors)this.hiddenErrors={};if(_2){if(isc.isA.Canvas(_2))_2=_2.getID()}else _2=this.getID();if(!this.hiddenErrors[_2])this.hiddenErrors[_2]={};for(var _3 in _1){this.hiddenErrors[_2][_3]=this.$43t(this.hiddenErrors[_2][_3],_1[_3])}}
,isc.A.getHiddenErrors=function(_1){if(!_1){this.synchHiddenErrors()}
if(!this.hiddenErrors)return null;var _2={};for(var _3 in this.hiddenErrors){isc.addProperties(_2,this.hiddenErrors[_3])}
return _2}
,isc.A.synchHiddenErrors=function(){var _1=this.hiddenErrors,_2=this.getID();if(_1&&_1[_2]){for(var _3 in _1[_2]){var _4=_1[_2][_3],_5=this.getItem(_3),_6=_5?_5.form:null;if(_5){_6.addFieldErrors(_3,_4);delete _1[_2][_3]}}}
var _7=_1[_2];_1=this.hiddenErrors={};if(_7)_1[_2]=_7;if(this.members){for(var i=0;i<this.members.length;i++){if(!isc.isA.DynamicForm(this.members[i]))continue;var _9=this.members[i],_10=_9.getID(),_11=_9.errors;if(!_11||isc.isAn.emptyObject(_11))continue;if(!_9.isVisible()||!_9.isDrawn()){_11=isc.addProperties({},_11);_1[_10]=_11}else{for(var _3 in _11){var _5=_9.getItem(_3);if(!_5){if(!_1[_2])_1[_2]={};_1[_2][_3]=_11[_3];delete _11[_3]}else if(!_5.visible){if(!_1[_10])_1[_10]={};_1[_10][_3]=_11[_3]}}}}}}
,isc.A.processValidator=function(_1,_2,_3,_4,_5){return isc.Validator.processValidator(_1,_2,_3,_4,_5)}
,isc.A.$21z=function(_1){var _2=this.getHiddenErrors(_1);if(_2==null||isc.getKeys(_2).length==0)return;var _3;if(this.handleHiddenValidationErrors){_3=this.handleHiddenValidationErrors(_2)}
if(_3==false)return;var _4="Validation failed with the following errors:";for(var _5 in _2){var _6=_2[_5];if(!isc.isAn.Array(_6))_6=[_6];if(_6.length==0)continue;_4+="\n"+_5+":";for(var i=0;i<_6.length;i++){_4+=(i==0?"- ":"\n - ")+_6[i]}}
this.logWarn(_4,"validation")}
,isc.A.setErrors=function(_1,_2){this.clearHiddenErrors();_1=isc.DynamicForm.formatValidationErrors(_1);var _3=(this.members?this.members.duplicate():[]);for(var i=0;i<_3.length;i++){if(!isc.isA.DynamicForm(_3[i]))continue;var _5=_3[i],_6=!_5.isVisible()||!_5.isDrawn(),_7=_5.getItems(),_8={},_9={};for(var j=0;j<_7.getLength();j++){var _11=_7[j],_12=_11.getFieldName();if(_1[_12]!=null){_8[_12]=_1[_12];if(_6||!_11.visible){_9[_12]=_1[_12]}
delete _1[_12]}}
_5.setErrors(_8,false);if(!isc.isAn.emptyObject(_9))
this.addHiddenErrors(_9,_5)}
this.addHiddenErrors(_1);if(_2)this.showErrors(true)}
,isc.A.$43t=function(_1,_2){if(!_1)return _2;if(!_2)return _1;if(!isc.isAn.Array(_1))_1=[_1];if(isc.isA.String(_2))_1.add(_2);else _1.addList(_2);return _1}
,isc.A.addFieldErrors=function(_1,_2,_3){var _4=true;var _5=this.getMemberForField(_1);if(_5!=null&&isc.isA.DynamicForm(_5)){_5.addFieldErrors(_1,_2,false);var _6=_5.getItem();if(_5.isVisible()&&_5.isDrawn()&&_6&&_6.visible){_4=false}}
if(_4){if(!this.hiddenErrors)this.hiddenErrors={};var _7=_5?_5.getID():this.getID();if(!this.hiddenErrors[_7])this.hiddenErrors[_7]={};this.hiddenErrors[_7][_1]=this.$43t(this.hiddenErrors[_7][_1],_2)}
if(_3)this.showFieldErrors(_1)}
,isc.A.setFieldErrors=function(_1,_2,_3){var _4=true;var _5=this.getMemberForField(_1);if(_5!=null&&isc.isA.DynamicForm(_5)){_5.setFieldErrors(_1,_2,false);var _6=_5.getItem();if(_5.isVisible()&&_5.isDrawn()&&_6&&_6.visible){_4=false}}
if(_4){if(!this.hiddenErrors)this.hiddenErrors={};this.hiddenErrors[_1]=_2}
if(_3)this.showFieldErrors(_1)}
,isc.A.clearErrors=function(_1){this.setErrors({},_1)}
,isc.A.clearFieldErrors=function(_1,_2){var _3=this.getMemberForField(_1);if(_3&&isc.isA.DynamicForm(_3))_3.clearFieldErrors(_1,_2);if(this.hiddenErrors)delete this.hiddenErrors[_1]}
,isc.A.getErrors=function(){var _1=isc.addProperties({},this.getHiddenErrors(true));if(this.members){for(var i=0;i<this.members.length;i++){if(!isc.isA.DynamicForm(this.members[i]))continue;isc.addProperties(_1,this.members[i].getErrors())}}
if(!isc.isA.emptyObject(_1))return _1
return null}
,isc.A.getFieldErrors=function(_1){var _2=this.getMemberForField(_1)
if(_2&&isc.isA.DynamicForm(_2))return _2.getFieldErrors(_1);if(this.hiddenErrors&&this.hiddenErrors[this.getID()])
return this.hiddenErrors[this.getID()][_1]}
,isc.A.hasErrors=function(){if(this.hiddenErrors&&!isc.isA.emptyObject(this.hiddenErrors)){for(var _1 in this.hiddenErrors){if(this.hiddenErrors[_1]&&!isc.isAn.emptyObject(this.hiddenErrors[_1]))
return true}}
if(this.members==null)return false;for(var i=0;i<this.members.length;i++){if(isc.isA.DynamicForm(this.members[i])&&this.members[i].hasErrors())return true}
return false}
,isc.A.hasFieldErrors=function(_1){var _2=this.getMemberForField(_1);if(_2&&isc.isA.DynamicForm(_2)&&_2.hasFieldErrors(_1))return true;var _3=this.getHiddenErrors(true);if(_3&&_3[_1]!=null)return true;return false}
,isc.A.showErrors=function(_1){if(this.members){for(var i=0;i<this.members.length;i++){if(!isc.isA.DynamicForm(this.members[i]))continue;if(!this.members[i].isDrawn()||!this.members[i].isVisible())continue;this.members[i].markForRedraw("ValuesManager validation errors")}}
if(this.hiddenErrors!=null){this.$21z(_1)}}
,isc.A.showFieldErrors=function(_1){var _2=this.getMemberForField(_1);if(_2&&isc.isA.DynamicForm(_2)&&_2.isVisible()&&_2.isDrawn()){var _3=_2.getItem(_1);if(_3&&_3.visible){_3.redraw("Validation errors modified");return}}
this.$21z()}
,isc.A.getFilterCriteria=function(){var _1={};if(this.members){for(var i=0;i<this.members.length;i++){isc.addProperties(_1,this.members[i].getFilterCriteria())}}
var _3=this.getValues(),_4;for(var _5 in _3){if(_1[_5]!==_4)delete _3[_5]}
isc.addProperties(_1,isc.DataSource.filterCriteriaForFormValues(_3));return _1}
,isc.A.getValues=function(){if(this.members!=null){var _1=isc.EH.getFocusCanvas();if(this.members.contains(_1)&&_1.updateFocusItemValue)_1.updateFocusItemValue()}
return isc.addProperties({},this.values)}
,isc.A.setValues=function(_1){if(isc.isAn.Array(_1)){var _2=isc.isA.Object(_1[0]);this.logWarn("values specified as an array."+(_2?" Treating the first item in the array as intended values.":" Ignoring specified values."));if(_2)_1=_1[0];else _1=null}
_1=isc.addProperties({},_1);this.values=_1;if(this.members){for(var i=0;i<this.members.length;i++){this.$707(this.members[i])}}
this.rememberValues()}
,isc.A.setData=function(_1){return this.setValues(_1)}
,isc.A.clearValues=function(){this.setValues({})}
,isc.A.getMemberValues=function(_1){var _2=this.getMember(_1);if(_2!=null)return _2.getValues()}
,isc.A.setMemberValues=function(_1,_2){var _3=this.getMember(_1);if(_3!=null)return _3.setValues(_2)}
,isc.A.rememberValues=function(){var _1=this.getValues();this.$10s={}
this.$10t=[]
this.$708(this.$10s,_1);return this.$10s}
,isc.A.$708=function(_1,_2,_3){var _4="__ref";for(var _5 in _2){if(isc.isA.Function(_2[_5]))continue;if(_5==_4)continue;var _6=_5;if(_3){_6=_3+_5}else{_6=_5}
var _7=this.getItem(_6);if(_7&&_7.isSetToDefaultValue()){this.$10t.add(_6)}
var _8=_2[_5];if(isc.isA.Date(_8)){_1[_5]=_8.duplicate()}else if(isc.isAn.Object(_8)&&!isc.isAn.Array(_8)){_1[_5]={};this.$708(_1[_5],_8,(_6+isc.Canvas.$70l))}else{_1[_5]=_2[_5]}}}
,isc.A.resetValues=function(){var _1={};for(var _2 in this.$10s){if(this.$10t.contains(_2))continue;if(isc.isA.Date(this.$10s[_2])){var _3=this.getValue(_2);if(isc.isA.Date(_3)){_3.setTime(this.$10s[_2].getTime())
_1[_2]=_3}else{_1[_2]=this.$10s[_2].duplicate()}}else{_1[_2]=this.$10s[_2]}}
this.setValues(_1)}
,isc.A.valuesHaveChanged=function(){return!isc.objectsAreEqual(this.getValues(),this.$10s)}
,isc.A.getValue=function(_1){return isc.DynamicForm.$70o(_1,this.values)}
,isc.A.setValue=function(_1,_2){var _3=false,_4,_5;if(this.members){var _6=this.getItem(_1);_4=_6&&_6.form?_6.form:this.getMemberForField(_1);if(_6&&_6.setValue){if(_2===_5)_6.clearValue();else _6.setValue(_2);_3=true}}
if(!_3){if(_2===_5)isc.DynamicForm.$70m(_1);else isc.DynamicForm.$70n(_1,_2,this.values);if(_4&&_4.setData){var _7=_1;if(_1.indexOf(isc.Canvas.$70l)!=-1){_7=_1.substring(0,_1.lastIndexOf(isc.Canvas.$70l));_4.setData(isc.DynamicForm.$70o(_7,this.values))}}}}
,isc.A.clearValue=function(_1){this.setValue(_1)}
,isc.A.addMember=function(_1,_2){if(isc.isA.String(_1))_1=window[_1];if(!isc.isA.Canvas(_1)){this.logWarn("addMember() passed invalid object: "+this.echo(_1)+" - this should be a DynamicForm instance");return}
if(_1.valuesManager!=null)_1.valuesManager.removeMember(_1);var _3=_1.getDataSource();if(_3!=null&&!_2&&_3!=this.getDataSource()){this.logWarn("addMember(): mismatched DataSources; new member form "+_1+" has dataSource: '"+_3.ID+"', valuesManager has DataSource '"+this.getDataSource().ID+"'")}
if(this.getDataSource()!=null&&_1.isMultipart&&_1.isMultipart()&&_1.isMultipart())
{this.logWarn("addMember(): new member form "+_1+" is flagged as using multipart encoding. Multipart forms require direct form "+"submission to transfer uploaded files to the server - any uploaded files from "+"this member form will be dropped when saving values from this ValuesManager to "+"the server.")}
if(_1.dataArity=="single"&&_1.autoTrackSelection){var _4=_1.getFullDataPath(),_5=_4?this.getDataSourceField(_4):null,_6=isc.DynamicForm.$70o(_4,this.values),_7=isc.isAn.Array(_6)||(_5&&_5.multiple);if(_7){var _8=this.getMemberForField(_4,true);if(_8&&_8.length>0){for(var i=0;i<_8.length;i++){var _10=_8[i];if(_10.dataArity=="multiple"){_1.setSelectionComponent(_10);break}}}}}else{var _4=_1.getFullDataPath(),_11=this.getMemberForField(_4,true);if(_11&&_11.length>0){for(var i=0;i<_11.length;i++){if(_11[i].dataArity=="single"&&_11[i].autoTrackSelection)
{_11[i].setSelectionComponent(_1)}}}}
if(this.members==null)this.members=[];this.members.add(_1);this.$707(_1,true);_1.valuesManager=this;_1.$703=_2;this.rememberValues()}
,isc.A.$707=function(_1,_2){if(_1.getFields==null)return;var _3=_1.getFullDataPath(),_4=this.getField(_3),_5=isc.DynamicForm.$70o(_3,this.values),_6=isc.isAn.Array(_5)||(_4&&_4.multiple);if(_6){if(_1.dataArity=="single"){if(_1.selectionComponent!=null)return;else if(isc.isAn.Array(_5))_5=_5[0]}}else{if(_5!=null&&_1.dataArity=="multiple")_5=[_5]}
if(!isc.isA.DynamicForm(_1)){if(!_1.setData)return;var _7=_1.getFullDataPath(),_8=_2?_1.getData():null;if(_5==null){if(_2)isc.DynamicForm.$70n(_7,_8)}else{if(_2&&_8!=null&&!isc.isAn.emptyObject(_8)&&!isc.isAn.emptyArray(_8))
{this.logInfo("ValuesManager member:"+_1.getID()+" has existing values:"+this.echo(_8)+", replacing with values from this valuesManager:"+this.echo(_5))}
_1.setData(_5)}}else{var _9=_1.getItems(),_10;for(var i=0;i<_9.getLength();i++){var _12=_9[i];if(!_12.shouldSaveValue)continue;var _13=_12.getDataPath()||_12.getFieldName();if(!_13)continue;var _14=_13,_3=_1.getFullDataPath();if(_3){_14=this.$702(_3,_14)}
var _15=isc.DynamicForm.$70o(_14,this.values);if(_15!==_10){this.logInfo("Member form "+_1+" has specified value for field '"+_13+"' which collides with an already specified value in this "+"ValuesManager. Resetting the value on the member form.");_1.setValue(_13,_15)}else{if(!_2)_1.clearValue(_13)}
var _16=_1.getValue(_13);if(_16===_10){isc.DynamicForm.$70m(_14,this.values)}else{isc.DynamicForm.$70n(_14,_1.getValue(_13),this.values)}}
if(_2){this.$709(_1)}}}
,isc.A.$709=function(_1,_2,_3,_4,_5){if(_2==null)_2=_1.getValues();if(_4==null)_4=[];for(var _6 in _2){var _7=_3?this.$702(_3,_6):_6;if(!_1.getItem(_7)){var _8=_2[_6];if(!isc.isAn.Object(_8)||isc.isA.Date(_8)||isc.isAn.Array(_8)){_4.add(_7);_1.clearValue(_7)}else{this.$709(_1,_8,_3,_4,true)}}}
if(!_5&&_4.length>0){this.$66q(_1,_4)}}
,isc.A.addMembers=function(_1){if(!isc.isAn.Array(_1))this.addMember(_1);else{for(var i=0;i<_1.length;i++){this.addMember(_1[i])}}}
,isc.A.removeMember=function(_1){if(isc.isA.String(_1)){_1=isc.Class.getArrayItem(_1,this.members);if(_1==null)return}else if(!this.members.contains(_1))return;this.members.remove(_1);delete _1.valuesManager}
,isc.A.removeMembers=function(_1){if(!isc.isAn.Array(_1))this.removeMember(_1);else{for(var i=0;i<_1.length;i++){this.removeMember(_1[i])}}}
,isc.A.getPrintHTML=function(){var _1=this.getValues(),_2=isc.StringBuffer.create();_2.append("<TABLE border=1><TR><TD align='center' style='font-weight:bold;'>Field</TD>","<TD align='center' style='font-weight:bold;'>Value</TD>");for(var _3 in _1){_2.append("<TR><TD>",_3,"</TD><TD>",_1[_3],"</TD></TR>")}
_2.append("</TABLE>");return _2.toString()}
);isc.B._maxIndex=isc.C+63;isc.ValuesManager.registerStringMethods({handleHiddenValidationErrors:"errors"});isc.defineClass("ColorPicker",isc.Window);isc.A=isc.ColorPicker;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.getSharedColorPicker=function(_1,_2){if(!isc.isA.ColorPicker(this.$56b)){this.$56b=isc.ColorPicker.create(_1)}
if(!_2){var _3=this.$56b;if(_3.$56c!=_3.defaultPickMode){_3.$56c=_3.defaultPickMode;if(_3.$56c=='simple'){_3.removeComplexElements();if(_3.allowComplexMode){_3.modeToggleButton.setTitle("More >>")}}else{if(!_3.$56i){_3.createComplexElements()}
_3.addComplexElements();_3.modeToggleButton.setTitle("<< Less")}}
_3.setHtmlColor(_3.defaultColor);_3.$56d=this.defaultOpacity}
return this.$56b}
);isc.B._maxIndex=isc.C+1;isc.addGlobal("ColorChooser",isc.ColorPicker);isc.A=isc.ColorPicker.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.title="Select a Color";isc.A.autoSize=true;isc.A.isModal=true;isc.A.autoCenter=true;isc.A.autoDraw=false;isc.A.showMinimizeButton=false;isc.A.layoutMargin=2;isc.A.canFocus=false;isc.A.showOkButton=true;isc.A.okButtonConstructor=isc.IButton;isc.A.okButtonDefaults={title:"OK",width:80,autoParent:"buttonLayout",click:function(){if(this.creator.colorSelected){this.creator.colorSelected(this.creator.getHtmlColor(),this.creator.getOpacity())}
this.creator.hide()}};isc.A.showCancelButton=true;isc.A.cancelButtonConstructor=isc.IButton;isc.A.cancelButtonDefaults={title:"Cancel",width:80,autoParent:"buttonLayout",click:function(){this.creator.hide()}};isc.A.showModeToggleButton=true;isc.A.modeToggleButtonConstructor=isc.IButton;isc.A.modeToggleButtonDefaults={title:"More >>",width:80,autoParent:"buttonLayout",click:function(){this.creator.$56e()}};isc.A.showButtonLayout=true;isc.A.buttonLayoutConstructor="HLayout";isc.A.buttonLayoutDefaults={autoParent:"contentLayout"};isc.A.defaultColor="#808080";isc.A.colorButtonSize=20;isc.A.colorButtonBaseStyle="colorChooserCell";isc.A.colorArray=["#000000","#996100","#636300","#006300","#006366","#000080","#636399","#636363","#800000","#FF6600","#808000","#8000FF","#008080","#0000FF","#666699","#808080","#FF0000","#FF9900","#99CC00","#639966","#63CCCC","#6366FF","#800080","#999999","#FF00FF","#FFCC00","#FFFF00","#00FF00","#00FFFF","#00CCFF","#996366","#C0C0C0","#FF99CC","#FFCC99","#FFFF99","#CCFFCC","#CCFFFF","#99CCFF","#CC99FF","#FFFFFF"];isc.A.swatchWidth=170;isc.A.swatchHeight=170;isc.A.lumStep=4;isc.A.lumWidth=15;isc.A.supportsTransparency=true;isc.A.opacityText="Lorem ipsum dolor sit amet, consectetuer adipiscing elit.";isc.A.swatchImageURL="[SKIN]ColorPicker/spectrum.png";isc.A.crosshairImageURL="[SKIN]ColorPicker/crosshair.png";isc.A.basicColorLabel="Basic Colors:";isc.A.selectedColorLabel="Selected Color:";isc.A.opacitySliderLabel="Opacity:";isc.A.defaultOpacity=100;isc.A.autoPosition=true;isc.A.autoCenterOnShow=true;isc.A.defaultPickMode="simple";isc.A.allowComplexMode=true;isc.A.$56f=true;isc.B.push(isc.A.closeClick=function(){this.hide()}
);isc.B._maxIndex=isc.C+1;isc.A=isc.ColorPicker.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.show=function(){if(this.autoPosition){this.autoCenter=false;var _1=isc.EH.getLastEvent();this.placeNear(_1.x,_1.y)}else{if(this.autoCenterOnShow)this.autoCenter=true}
this.Super("show",arguments)}
,isc.A.initWidget=function(){this.$56c=this.defaultPickMode;this.basicColorLayout=isc.VLayout.create({autoDraw:false});for(var i=0;i<5;i++){var _2=isc.HLayout.create({autoDraw:false,layoutBottomMargin:1,membersMargin:1,height:this.colorButtonSize});for(var j=0;j<8;j++){var _4=isc.StatefulCanvas.create({autoDraw:false,width:this.colorButtonSize,height:this.colorButtonSize,overflow:"hidden",title:"",backgroundColor:this.colorArray[i*8+j],baseStyle:this.colorButtonBaseStyle,showRollOver:true,picker:this,click:function(){this.picker.setHtmlColor(this.backgroundColor);if(this.picker.$56c=="simple"){this.picker.$56g(this.backgroundColor)}}});_2.addMember(_4)}
this.basicColorLayout.addMember(_2)}
this.leftHandLayout=isc.VLayout.create({autoDraw:false});this.leftHandLayout.addMember(this.basicColorLayout);this.innerContentLayout=isc.HLayout.create({autoDraw:false,align:"center",members:[this.leftHandLayout]});this.contentLayout=isc.VLayout.create({autoDraw:false,members:[this.innerContentLayout]});this.addItem(this.contentLayout);if(this.$56c=="simple"){this.showOkButton=false;if(!this.allowComplexMode){this.showModeToggleButton=false}}else{this.showModeToggleButton=false}
this.addAutoChildren(["buttonLayout","okButton","cancelButton","modeToggleButton"]);if(this.$56c=="complex"){this.createComplexElements();this.addComplexElements()}
this.setHtmlColor(this.defaultColor);this.$56h();this.setOpacity(this.defaultOpacity);this.Super("initWidget",arguments)}
,isc.A.createComplexElements=function(){if(this.$56c!='complex'){return}
this.$56i=isc.DynamicForm.create({autoDraw:false,cellPadding:1,padding:10,width:65,fields:[{name:"pickerRedVal",title:"Red",type:"text",width:"40",defaultValue:this.$56j,prompt:"The Red component of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setRed(_4)}},{name:"pickerGrnVal",title:"Green",type:"text",width:"40",defaultValue:this.$56k,prompt:"The Green component of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setGreen(_4)}},{name:"pickerBluVal",title:"Blue",type:"text",width:"40",defaultValue:this.$56l,prompt:"The Blue component of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setBlue(_4)}},{name:"pickerHtmlVal",title:"HTML",type:"text",width:"65",defaultValue:this.$56m,prompt:"The selected color's HTML coding",picker:this,changed:function(_2,_3,_4){this.picker.setHtmlColor(_4)}}]});this.$56n=isc.DynamicForm.create({autoDraw:false,cellPadding:1,padding:10,width:65,fields:[{name:"pickerHueVal",title:"Hue",type:"text",width:"40",defaultValue:this.$56o,prompt:"The Hue (base tone) of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setHue(_4)}},{name:"pickerSatVal",title:"Sat",type:"text",width:"40",defaultValue:this.$56p,prompt:"The Saturation (color purity) of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setSaturation(_4)}},{name:"pickerLumVal",title:"Lum",type:"text",width:"40",defaultValue:this.$56q,prompt:"The Luminosity (brightness) of the selected color",picker:this,changed:function(_2,_3,_4){this.picker.setLuminosity(_4)}}]});this.$56r=isc.Img.create({autoDraw:false,imageWidth:16,imageHeight:16,src:this.crosshairImageURL,width:16,height:16,imageType:"normal",canDrag:true,canDrop:true,dragAppearance:"target",picker:this,dragMove:function(){this.picker.$56s=true;this.picker.$56t(this.parentElement.getOffsetX(),this.parentElement.getOffsetY())}});this.$56u=isc.Canvas.create({autoDraw:false,width:100,height:40,backgroundColor:this.getHtmlColor()});this.$56v=isc.Canvas.create({autoDraw:false,width:60,height:40,overflow:"hidden",border:"1px black solid",contents:this.opacityText,children:[this.$56u]});this.$56w=isc.VStack.create({lumWidth:15,height:this.swatchHeight,margin:5,border:"1px solid black"});for(var i=0;i<this.swatchHeight/ this.lumStep;i++){this.$56w.addMember(isc.Canvas.create({width:this.lumWidth,height:this.lumStep,margin:0,padding:0,overflow:"hidden"}))}
this.$56x=isc.Slider.create({minValue:0,maxValue:240,numValues:240,margin:5,length:this.swatchHeight,width:10,showTitle:false,showValue:false,showRange:false});if(this.supportsTransparency){this.$56y=isc.Slider.create({autoDraw:false,vertical:false,margin:5,minValue:0,maxValue:100,numValues:100,length:100,height:12,width:100,thumbThickWidth:15,thumbThinWidth:10,showTitle:false,showValue:false,showRange:false,value:100});this.$56z=isc.HLayout.create({autoDraw:false,layoutLeftMargin:5,layoutRightMargin:5,membersMargin:5,members:[isc.Label.create({autoDraw:false,margin:5,contents:this.opacitySliderLabel,width:this.swatchWidth-105,height:10}),this.$56y]})}
this.$560=isc.VLayout.create({autoDraw:false,layoutLeftMargin:5,layoutRightMargin:5,membersMargin:5,members:[isc.HLayout.create({autoDraw:false,height:this.swatchHeight,members:[isc.Img.create({autoDraw:false,margin:5,width:this.swatchWidth+12,height:this.swatchHeight+12,src:this.swatchImageURL,overflow:"hidden",border:"1px black solid",picker:this,click:function(){this.picker.$56t(this.getOffsetX(),this.getOffsetY())},children:[this.$56r]}),this.$56w,this.$56x]}),isc.HLayout.create({autoDraw:false,layoutLeftMargin:5,layoutRightMargin:5,membersMargin:5,members:[isc.Label.create({autoDraw:false,margin:5,contents:this.selectedColorLabel,width:this.swatchWidth-63,height:15}),this.$56v]})]});this.observe(this.$56x,"valueChanged","observer.$561()");this.observe(this.$56y,"valueChanged","observer.$562()")}
,isc.A.initComplexElements=function(){this.$56x.setValue(this.$56q);this.$56h();this.$563(this.$56o,this.$56p);this.$56u.setBackgroundColor(isc.ColorUtils.hslToHtml(this.$56o,this.$56p,this.$56q));if(this.supportsTransparency){this.$56u.setOpacity(this.$56d);this.$56y.setValue(this.$56d)}}
,isc.A.addComplexElements=function(){if(this.$56c!='complex'){return}
this.showOkButton=true;this.setAutoChild("okButton");this.basicLabel=isc.Label.create({autoDraw:false,margin:5,contents:this.basicColorLabel,width:100,height:15});this.formLayout=isc.HLayout.create({autoDraw:false,members:[this.$56i,this.$56n]});this.leftHandLayout.addMember(this.basicLabel,0);this.leftHandLayout.addMember(this.formLayout);if(this.supportsTransparency){this.$560.addMember(this.$56z)}
this.innerContentLayout.addMember(this.$560);this.initComplexElements()}
,isc.A.removeComplexElements=function(){if(this.$56c=='complex'){return}
this.showOkButton=false;this.setAutoChild("okButton");if(this.formLayout){this.leftHandLayout.removeMembers([this.basicLabel,this.formLayout]);this.innerContentLayout.removeMember(this.$560)}}
,isc.A.setSupportsTransparency=function(_1){this.supportsTransparency=_1;if(this.$56c=='complex'){if(this.supportsTransparency){this.$560.addMember(this.$56z)}else{this.$560.removeMember(this.$56z)}}}
,isc.A.getRed=function(){return this.$56j}
,isc.A.getGreen=function(){return this.$56k}
,isc.A.getBlue=function(){return this.$56l}
,isc.A.getHue=function(){return this.$56o}
,isc.A.getSaturation=function(){return this.$56p}
,isc.A.getLuminosity=function(){return this.$56q}
,isc.A.getHtmlColor=function(){return this.$56m}
,isc.A.getOpacity=function(){return this.$56d}
,isc.A.setRed=function(_1){if(_1<0)this.$56j=0;else if(_1>255)this.$56j=255;else this.$56j=_1/ 1;if(this.$56c=='complex'){this.$56i.setValue("pickerRedVal",this.$56j)}
if(this.$56f===true)
this.$564('rgb')}
,isc.A.setGreen=function(_1){if(_1<0)this.$56k=0;else if(_1>255)this.$56k=255;else this.$56k=_1/ 1;if(this.$56c=='complex'){this.$56i.setValue("pickerGrnVal",this.$56k)}
if(this.$56f===true)
this.$564('rgb')}
,isc.A.setBlue=function(_1){if(_1<0)this.$56l=0;else if(_1>255)this.$56l=255;else this.$56l=_1/ 1;if(this.$56c=='complex'){this.$56i.setValue("pickerBluVal",this.$56l)}
if(this.$56f===true)
this.$564('rgb')}
,isc.A.setHue=function(_1){if(_1<0)this.$56o=0;else if(_1>239)this.$56o=239;else this.$56o=_1/ 1;if(this.$56c=='complex'){this.$56n.setValue("pickerHueVal",this.$56o)}
if(this.$56f===true)
this.$564('hsl')}
,isc.A.setSaturation=function(_1){if(_1<0)this.$56p=0;else if(_1>240)this.$56p=240;else this.$56p=_1/ 1;if(this.$56c=='complex'){this.$56n.setValue("pickerSatVal",this.$56p)}
if(this.$56f===true)
this.$564('hsl')}
,isc.A.setLuminosity=function(_1){if(_1<0)this.$56q=0;else if(_1>240)this.$56q=240;else this.$56q=_1/ 1;if(this.$56c=='complex'){this.$56n.setValue("pickerLumVal",this.$56q)}
if(this.$56f===true)
this.$564('hsl')}
,isc.A.setHtmlColor=function(_1){if(isc.ColorUtils.encodingIsValid(_1)===true){this.$56m=_1.toUpperCase();if(this.$56c=='complex'){this.$56i.setValue("pickerHtmlVal",this.$56m)}
if(this.$56f===true)
this.$564('html')}}
,isc.A.setOpacity=function(_1){if(this.$56c=='complex'&&this.supportsTransparency){if(_1<0)this.$56d=0;else if(_1>100)this.$56d=100;else this.$56d=_1/ 1;if(this.$56f===true)
this.$564('opacity')}}
,isc.A.$564=function(_1){if(_1=='rgb'){var _2=isc.ColorUtils.rgbToHsl(this.$56j,this.$56k,this.$56l);this.$56f=false;this.setHue(_2.h);this.setSaturation(_2.s);this.setLuminosity(_2.l);this.setHtmlColor(isc.ColorUtils.rgbToHtml(this.$56j,this.$56k,this.$56l));this.$56f=true;this.$563(this.$56o,this.$56p)}else if(_1=='hsl'){var _3=isc.ColorUtils.hslToRgb(this.$56o,this.$56p,this.$56q);this.$56f=false;this.setRed(_3.r);this.setGreen(_3.g);this.setBlue(_3.b);this.setHtmlColor(isc.ColorUtils.rgbToHtml(this.$56j,this.$56k,this.$56l));this.$56f=true;if(this.$56o!=this.$565||this.$56p!=this.$566){this.$563(this.$56o,this.$56p)}}else if(_1=='html'){var _3=isc.ColorUtils.htmlToRgb(this.$56m);this.$56f=false;this.setRed(_3.r);this.setGreen(_3.g);this.setBlue(_3.b);var _2=isc.ColorUtils.rgbToHsl(this.$56j,this.$56k,this.$56l);this.setHue(_2.h);this.setSaturation(_2.s);this.setLuminosity(_2.l);this.$56f=true;this.$563(this.$56o,this.$56p)}
if(this.$56c=='complex'){this.$56x.setValue(this.$56q)}
if(this.$56c=='complex'){this.$56u.setBackgroundColor(isc.ColorUtils.hslToHtml(this.$56o,this.$56p,this.$56q))}
if(this.$56o!=this.$565||this.$56p!=this.$566){this.$56h()}
if(this.$56c=='complex')
this.$56u.setOpacity(this.$56d);this.$565=this.$56o;this.$566=this.$56p;if(this.colorChanged)this.colorChanged()}
,isc.A.$56g=function(_1){this.hide();if(this.colorSelected)this.colorSelected(_1)}
,isc.A.$563=function(_1,_2){if(this.$56c!='complex'){return}
if(this.$56s===true){this.$56s=false;return}
var _3=_1/ 239.0;var _4=_2/ 240.0;_3*=this.swatchWidth;_4=this.swatchHeight-(_4*this.swatchHeight);_3=parseInt(_3)-8;_4=parseInt(_4)-8;this.$56r.setLeft(_3);this.$56r.setTop(_4)}
,isc.A.$56t=function(_1,_2){_1-=5;_2-=5;_1/=this.swatchWidth;_2=1.0-_2/ this.swatchHeight;this.$56f=false;this.setHue(Math.floor(_1*239.0+0.5));this.$56f=true;this.setSaturation(Math.floor(_2*240.0+0.5))}
,isc.A.$56h=function(){if(this.$56c!='complex'){return}
for(var i=0;i<this.swatchHeight/ this.lumStep;i++){this.$56w.members[i].setBackgroundColor(isc.ColorUtils.hslToHtml(this.$56o,this.$56p,240-(i*240/(this.swatchHeight/ this.lumStep))))}}
,isc.A.$561=function(){var _1=this.$56x.getValue();if(this.$56q!=_1){this.setLuminosity(_1)}}
,isc.A.$562=function(){this.setOpacity(this.$56y.getValue())}
,isc.A.$56e=function(){if(this.$56c=="simple"){this.$56c="complex";if(!this.$560){this.createComplexElements()}
this.addComplexElements();this.modeToggleButton.setTitle("<< Less")}else{this.$56c="simple";this.removeComplexElements();this.modeToggleButton.setTitle("More >>")}
this.modeToggleButton.setState("")}
);isc.B._maxIndex=isc.C+31;isc.ColorPicker.registerStringMethods({colorChanged:"",colorSelected:"color,opacity"});isc.defineClass("ColorUtils",isc.Class);isc.A=isc.ColorUtils;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.hexToDec=function(_1){return parseInt(_1,16)}
,isc.A.decToHex=function(_1){var d=_1/ 1;var h=d.toString(16);if(h.length==1){h="0"+h}
return h}
,isc.A.brightness=function(_1,_2,_3){var _4=isc.ColorUtils.rgbToHsl(_1,_2,_3);return(_4.l/ 240.0)}
,isc.A.encodingIsValid=function(_1){return(_1.substring(0,1)=='#'&&isc.isA.color(_1))}
,isc.A.rgbToHtml=function(_1,_2,_3){var _4='#'+isc.ColorUtils.decToHex(_1)+isc.ColorUtils.decToHex(_2)+isc.ColorUtils.decToHex(_3);return _4}
,isc.A.hslToHtml=function(_1,_2,_3){var _4=isc.ColorUtils.hslToRgb(_1,_2,_3);var _5='#'+isc.ColorUtils.decToHex(_4.r)+isc.ColorUtils.decToHex(_4.g)+isc.ColorUtils.decToHex(_4.b);return _5}
,isc.A.htmlToRgb=function(_1){var r=_1.substring(1,3);var g=_1.substring(3,5);var b=_1.substring(5,7);return{r:isc.ColorUtils.hexToDec(r),g:isc.ColorUtils.hexToDec(g),b:isc.ColorUtils.hexToDec(b)}}
,isc.A.htmlToHsl=function(_1){var r=_1.substring(1,3);var g=_1.substring(3,5);var b=_1.substring(5,7);return isc.ColorUtils.rgbToHsl(isc.ColorUtils.hexToDec(r),isc.ColorUtils.hexToDec(g),isc.ColorUtils.hexToDec(b))}
,isc.A.rgbToHsl=function(_1,_2,_3){var _4=_1/ 255.0;var _5=_2/ 255.0;var _6=_3/ 255.0;var _7=Math.min(Math.min(_4,_5),_6);var _8=Math.max(Math.max(_4,_5),_6);var _9=_8-_7;var h=0,s=0,l=0;l=(_8+_7)/2.0;if(_8==_7){s=0;h=0}else{if(l<0.5){s=(_8-_7)/(_8+_7)}else{s=(_8-_7)/(2.0-_8-_7)}
if(_4==_8)
h=(_5-_6)/_9;else if(_5==_8)
h=2+(_6-_4)/_9;else
h=4+(_4-_5)/_9}
h=Math.floor(h*40+0.5);if(h<0)h+=240;s=Math.floor(s*240+0.5);l=Math.floor(l*240+0.5);return{h:h,s:s,l:l}}
,isc.A.hslToRgb=function(_1,_2,_3){var _4=_1/ 239.0;var _5=_2/ 240.0;var _6=_3/ 240.0;var _7,_8,_9,_10,_11;var r=0,g=0,b=0;if(_5==0){r=_6;g=_6;b=_6}else{if(_6<0.5){_8=_6*(1.0+_5)}else{_8=(_6+_5)-(_6*_5)}
_7=(2.0*_6)-_8;_9=_4+0.3333;_10=_4;_11=_4-0.3333;if(_9<0)_9+=1.0;if(_10<0)_10+=1.0;if(_11<0)_11+=1.0;if(_9>1)_9-=1.0;if(_10>1)_10-=1.0;if(_11>1)_11-=1.0;if(_9*6.0<1)
r=_7+(_8-_7)*6.0*_9;else if(_9*2.0<1)
r=_8;else if(_9*3.0<2)
r=_7+(_8-_7)*(0.6667-_9)*6.0;else
r=_7;if(_10*6.0<1)
g=_7+(_8-_7)*6.0*_10;else if(_10*2.0<1)
g=_8;else if(_10*3.0<2)
g=_7+(_8-_7)*(0.6667-_10)*6.0;else
g=_7;if(_11*6.0<1)
b=_7+(_8-_7)*6.0*_11;else if(_11*2.0<1)
b=_8;else if(_11*3.0<2)
b=_7+(_8-_7)*(0.6667-_11)*6.0;else
b=_7}
r=Math.floor(r*255.0+0.5);g=Math.floor(g*255.0+0.5);b=Math.floor(b*255.0+0.5);return{r:r,g:g,b:b}}
);isc.B._maxIndex=isc.C+10;isc.ClassFactory.defineClass("NestedEditorItem","CanvasItem");isc.A=isc.NestedEditorItem.getPrototype();isc.A.shouldSaveValue=true;isc.A.editorConstructor="DynamicForm";isc.A.editorDefaults={itemChanged:function(_1,_2){this.creator.updateValue(this.getValuesAsCriteria())}};isc.A=isc.NestedEditorItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.$67h();this.Super("init",arguments)}
,isc.A.isEditable=function(){return true}
,isc.A.$67h=function(){var _1;var _2={};if(this.form.dataSource){_1=isc.DataSource.getDataSource(this.form.dataSource);var _3=_1.getField(this.name);if(_3){_2.dataSource=_1.getFieldDataSource(_3)}}
if(this.form&&this.form.showComplexFieldsRecursively){_2.showComplexFields=true;_2.showComplexFieldsRecursively=true}else{_2.showComplexFields=false}
this.addAutoChild("editor",_2);this.canvas=this.editor}
,isc.A.setValue=function(_1){this.editor.setValues(_1)}
,isc.A.updateValue=function(_1){this.$10y(_1)}
);isc.B._maxIndex=isc.C+5;isc.ClassFactory.defineClass("NestedListEditorItem","CanvasItem");isc.A=isc.NestedListEditorItem.getPrototype();isc.A.shouldSaveValue=true;isc.A.editorConstructor="ListEditor";isc.A.editorDefaults={inlineEdit:false,height:155,saveRecord:function(){if(!this.form.validate())return false;var _1=this.form.getValues();this.showList();if(this.inlineEdit){this.grid.setEditValues(this.grid.getEditRow(),_1)}else{if(this.form.saveOperationType=="add"){this.grid.addData(_1)}else{isc.addProperties(this.currentRecord,_1);this.grid.markForRedraw()}
if(!this.inlineEdit){this.form.clearValues()}
this.creator.updateValue(this.grid.data)}
return true}};isc.A=isc.NestedListEditorItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.init=function(){this.$67h();this.Super("init",arguments)}
,isc.A.isEditable=function(){return true}
,isc.A.$67h=function(){var _1;var _2={};if(this.form.dataSource){_1=isc.DataSource.getDataSource(this.form.dataSource);var _3=_1.getField(this.name);if(_3){_2.dataSource=_1.getFieldDataSource(_3)}}
if(this.form&&this.form.showComplexFieldsRecursively){_2.formProperties={showComplexFields:true,showComplexFieldsRecursively:true};_2.gridProperties={showComplexFields:true,showComplexFieldsRecursively:true,canRemoveRecords:true,saveLocally:true,data:[]}}else{_2.formProperties={showComplexFields:false};_2.gridProperties={showComplexFields:false,canRemoveRecords:true,saveLocally:true}}
this.addAutoChild("editor",_2);this.canvas=this.editor}
,isc.A.updateValue=function(_1){this.editor.setData(_1);this.$10y(_1)}
,isc.A.setValue=function(_1){this.editor.setData(_1);this.Super("setValue",arguments)}
);isc.B._maxIndex=isc.C+5;isc.ClassFactory.defineClass("ViewFileItem","CanvasItem");isc.A=isc.ViewFileItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.shouldSaveValue=false;isc.A.colSpan="*";isc.A.height=20;isc.A.width="*";isc.A.overflow="visible";isc.A.canvasDefaults={_constructor:"Canvas",height:10,width:"100%"};isc.B.push(isc.A.isEditable=function(){return false}
,isc.A.init=function(){this.addAutoChild("canvas");this.Super('init',arguments)}
,isc.A.setValue=function(_1){var _2=this.form,_3=_2.getValues();if(this.type=="imageFile"&&this.showFileInline!=false){this.canvas.setHeight("*");this.canvas.setWidth("*");this.canvas.setContents(this.getImageHTML())}else{if(this.showFileInline==true){this.logWarn("setValue(): Unsupported field-type for showFileInline: "+this.type)}
this.canvas.setHeight(20);this.canvas.setWidth("*");this.canvas.setContents(this.getViewDownloadHTML(_1,_3))}
this.Super("setValue",arguments)}
,isc.A.getViewDownloadHTML=function(_1,_2){if(isc.isA.String(_1))return _1;if(_2==null)return null;var _3=_2[this.name+"_filename"];if(_3==null||isc.isA.emptyString(_3))return this.emptyCellValue;var _4=isc.Canvas.imgHTML("[SKIN]actions/view.png",16,16,null,"style='cursor:"+isc.Canvas.HAND+"' onclick='"+this.getID()+".viewFile()'");var _5=isc.Canvas.imgHTML("[SKIN]actions/download.png",16,16,null,"style='cursor:"+isc.Canvas.HAND+"' onclick='"+this.getID()+".downloadFile()'");return"<nobr>"+_4+"&nbsp;"+_5+"&nbsp;"+_3+"</nobr>"}
,isc.A.getImageHTML=function(){var _1=this.form.getValues(),_2=this.form.getField(this.name),_3=this.name+"$68c",_4;if(!_1[this.name])return" ";if(!_1[_3]){var _5=isc.Canvas.getFieldImageDimensions(_2,_1);_4=_1[_3]=isc.Canvas.imgHTML(this.form.getDataSource().streamFile(_1,_2.name),_5.width,_5.height)}else
_4=_1[_3];return _4}
,isc.A.viewFile=function(){isc.DS.get(this.form.dataSource).viewFile(this.form.getValues(),this.name)}
,isc.A.downloadFile=function(){isc.DS.get(this.form.dataSource).downloadFile(this.form.getValues(),this.name)}
);isc.B._maxIndex=isc.C+7;isc.defineClass("PanelHeader","ImgSectionHeader");isc.A=isc.PanelHeader.getPrototype();isc.A.width="100%";isc.A.height=22;isc.A.baseStyle="sectionHeader";isc.A=isc.PanelHeader.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.showSelectedIcon=false;isc.A.showRollOverIcon=false;isc.A.showDisabledIcon=false;isc.A.showDownIcon=false;isc.A.showFocusedIcon=false;isc.B.push(isc.A.initWidget=function(){this.setPanelTitleFromCanvas()}
,isc.A.setPanelTitleFromCanvas=function(){if(this.canvas){if(this.canvas.title)this.title=this.canvas.title;if(this.canvas.icon)this.icon=this.canvas.icon}}
);isc.B._maxIndex=isc.C+2;isc.PanelHeader.registerStringMethods({iconClick:""});isc.A=isc.Canvas.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.panelHeaderDefaults={_constructor:"PanelHeader"};isc.A.panelHeaderProperties={};isc.A.panelHeaderPlacement="peer";isc.B.push(isc.A.setupPanelHeader=function(){if(!this.showPanelHeader)return;this.panelHeader=this.createAutoChild("panelHeader",{canvas:this,snapTo:"T"});if(isc.isA.Layout(this))this.panelHeaderPlacement="member";else this.panelHeaderPlacement="peer";this.panelHeaderPlacement="peer";if(this.panelHeaderPlacement=="member"){this.addMember(this.panelHeader,0)}
else if(this.panelHeaderPlacement=="peer"){this.addPeer(this.panelHeader);this.panelHeader.moveAbove(this);this.$wi(this.panelHeader,isc.Canvas.TOP)}
else if(this.panelHeaderPlacement=="custom"){}}
);isc.B._maxIndex=isc.C+1;isc.A=isc.Canvas.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.panelActionControls=[];isc.B.push(isc.A.refreshPanelControls=function(){var _1=this.panelControls||[];if(!this.panelHeader.controls)this.panelHeader.controls=[];else this.panelHeader.controls.setLength(0);for(var i=0;i<_1.length;i++){var _3=_1.get(i),_4=null;if(isc.isAn.Object(_3)){_4=isc.addProperties({},_3)}else if(_3.startsWith("action:")){var _5=_3.substring(7,_3.length),_6=isc.Canvas.getRegisteredAction(_5);if(this.canPerformAction(_6)){if(this.showActionInPanel(_6))
_4=this.getPanelActionControl(_6)}}else{_4=this.createAutoChild(_3)}
if(_4){this.panelHeader.controls.add(_4)}else{}}
var _7=this.panelHeader;_7.addControls()}
,isc.A.canPerformAction=function(_1){var _2=_1.name,_3=_1.enableProperty||"can"+_2.substring(0,1).toUpperCase()+_2.substring(1,_2.length);return this[_3]&&this[_3]==true?true:false}
,isc.A.showActionInPanel=function(_1){return _1.showInPanel}
,isc.A.getPanelActionControl=function(_1){var _2=this.panelActionControls[_1.name],_3=_1.controlConstructor||"ImgButton";_2=isc.ClassFactory.newInstance(_3,{ID:this.getID()+"_"+_1.name,width:18,height:18,src:_1.icon,showRollOver:false,showDown:false,showDisabled:false,showFocused:false,actionTarget:this,actionObject:_1,prompt:_1.tooltip,click:function(){this.actionTarget[this.actionObject.methodName]()}});this.panelActionControls[_1.name]=_2;return this.panelActionControls[_1.name]}
,isc.A.showPrintPreview=function(_1,_2,_3,_4){isc.Canvas.showPrintPreview(this,_1,_2,_3,_4)}
);isc.B._maxIndex=isc.C+5;isc.A=isc.Canvas;isc.A.$71k={};isc.A=isc.Canvas;isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.B.push(isc.A.registerAction=function(_1){if(!this.$71k[_1.name]){this.$71k[_1.name]=_1}}
,isc.A.getRegisteredActionNames=function(){return isc.getKeys(this.$71k)}
,isc.A.getRegisteredActions=function(){return isc.getValues(this.$71k)}
,isc.A.getRegisteredActionIndex=function(){var _1=this.getRegisteredActions(),_2=_1.makeIndex("name",false);return _2}
,isc.A.getRegisteredAction=function(_1){return this.$71k[_1]}
,isc.A.isActionRegistered=function(_1){return!this.$71k[_1]?false:true}
);isc.B._maxIndex=isc.C+6;isc.defineClass("Action","Class");isc.A=isc.Action.getPrototype();isc.A.name=null;isc.A.title=null;isc.A.icon=null;isc.A.tooltip=null;isc.A.methodName=null;isc.A.controlConstructor="ImgButton";isc.A.enableProperty=null;isc.A.showInPanel=true;isc.Canvas.registerAction(isc.Action.create({name:"edit",title:"Edit",icon:"[SKINIMG]/actions/edit.png",tooltip:"Put the component into Edit mode",methodName:"startEditing",showInPanel:false}));isc.Canvas.registerAction(isc.Action.create({name:"editNew",title:"Edit New",icon:"[SKINIMG]/SectionHeader/opener_closed.png",tooltip:"Add a new Record to the component",methodName:"startEditingNew",showInPanel:false}));isc.Canvas.registerAction(isc.Action.create({name:"sort",title:"Sort",icon:"[SKINIMG]/actions/sort_ascending.png",tooltip:"Sort the records in the component",methodName:"sort",controlConstructor:"SortActionSelector",enableProperty:"canSortFields",showInPanel:false}));isc.Canvas.registerAction(isc.Action.create({name:"export",title:"Export",icon:"[SKINIMG]/actions/redo.png",tooltip:"Export the data in the component",methodName:"exportData",showInPanel:true}));isc.Canvas.registerAction(isc.Action.create({name:"print",title:"Print",icon:"[SKINIMG]/actions/print.png",tooltip:"Print the data in the component",methodName:"showPrintPreview",showInPanel:true}));isc.defineClass("SortActionSelector","DynamicForm");isc.A=isc.SortActionSelector.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.width=100;isc.A.height=20;isc.A.numCols=4;isc.A.fields=[{name:"sortField",showTitle:false,colSpan:2,type:"select",width:80,prompt:"Sort Field",startRow:false,endRow:false,changed:function(_1,_2,_3){var _4=_1.getField("sortDirection").getValue(),_5=_4?"ascending":"descending";if(_1.actionTarget.sort)_1.actionTarget.sort(_3,_5);else _1.actionTarget.data.sortByProperty(_3,_4)}},{name:"sortDirection",showTitle:true,showLabel:false,type:"checkbox",width:20,prompt:"Sort Direction: Checked is Ascending",startRow:false,endRow:false,changed:function(_1,_2,_3){var _4=_3,_5=_4?"ascending":"descending",_6=_1.getField("sortField").getValue();if(_1.actionTarget.sort)_1.actionTarget.sort(_6,_5);else _1.actionTarget.data.sortByProperty(_6,_4)}}];isc.B.push(isc.A.initWidget=function(){this.Super("initWidget",arguments)}
,isc.A.draw=function(){this.Super("draw",arguments);var _1=this.actionTarget.getDataSource(),_2=_1?isc.getValues(_1.getFields()):[],_3={};for(var i=0;i<_2.length;i++){var _5=_2.get(i);_3[_5.name]=_5.title}
this.getField("sortField").setValueMap(_3)}
);isc.B._maxIndex=isc.C+2;isc.defineClass("DataPathItem","TextItem");isc.A=isc.DataPathItem.getPrototype();isc.B=isc._allFuncs;isc.C=isc.B._maxIndex;isc.D=isc._funcClasses;isc.D[isc.C]=isc.A.Class;isc.A.operationsTreeDefaults={_constructor:"TTreeGrid",autoDraw:false,recordDoubleClick:function(){this.creator.operationSelected()},getIcon:function(_1){var _2=this.creator.form.creator,_3=(_2&&_2.getServiceElementIcon)?_2.getServiceElementIcon(_1):null;if(_3)return _3;return this.Super("getIcon",arguments)}};isc.A.operationsTreeSelectButtonDefaults={_constructor:"TButton",autoDraw:false,title:"Select",click:function(){if(this.creator.operationsTree.anySelected())
this.creator.operationSelected()}};isc.A.defaultIcons=[{src:"[SKINIMG]/actions/edit.png",click:"item.showOperationsTreeData()",width:16,height:16},{src:"[SKINIMG]/actions/remove.png",click:"item.clearFormValues()",width:16,height:16}];isc.A.baseManagedProperties=["dataPath","schemaDataSource","serviceName","serviceNamespace"];isc.B.push(isc.A.getPropertyName=function(_1){if(this.isInput)
return"input"+_1.substring(0,1).toUpperCase()+_1.substring(1);else return _1}
,isc.A.initManagedProperties=function(){this.managedProperties=[];var _1=this.baseManagedProperties;for(var i=0;i<_1.length;i++){this.managedProperties.add(this.getPropertyName(_1[i]))}}
,isc.A.keyPress=function(_1,_2,_3){if(_3!="Arrow_Left"&&_3!="Arrow_Right"&&_3!="Home"&&_3!="End")return false;this.Super("keyPress",arguments)}
,isc.A.init=function(){this.icons=isc.clone(this.defaultIcons);this.initManagedProperties();this.Super("init",arguments);if(this.operationsTreeData){this.addAutoChildren(["operationsTree","operationsTreeSelectButton"])}}
,isc.A.showOperationsTreeData=function(){if(!this.operationsTreeData)return;if(!this.operationsTree)
this.addAutoChildren(["operationsTree","operationsTreeSelectButton"]);var _1;if(isc.isA.Tree(this.operationsTreeData)){_1=this.operationsTreeData}else{_1=isc.Tree.create({modelType:"children",root:{children:this.operationsTreeData},nameProperty:"name",childrenProperty:"children"});_1.openAll()}
this.operationsTree.setData(_1);var _2=this.isInput?"formInputs":"formOutputs";var _3=_2+"/"+this.getValue(),_4=_1.find(_3);if(_4)this.operationsTree.selectRecord(_4);if(!this.schemaDialog){this.schemaDialog=isc.TWindow.create({title:"Select element from message",autoCenter:true,height:"90%",width:"60%",isModal:true,showModalMask:true,items:[isc.VLayout.create({width:"100%",height:"100%",members:[this.operationsTree,this.operationsTreeSelectButton]})]})}else this.schemaDialog.show()}
,isc.A.operationSelected=function(){var _1=this.operationsTree,_2=_1.data,_3=_1.getSelectedRecord();this.schemaDialog.hide();this.setDataPathProperties(_3)}
,isc.A.setDataPathProperties=function(_1){var _2=_1.initData||_1.defaults;this.dataPathProps=isc.applyMask(_2,this.managedProperties);if(this.logIsInfoEnabled()){this.logInfo("setDPProps, editNode: "+this.echoAll(_1)+" defaults: "+this.echo(_2)+", managedProps: "+this.managedProperties+", props: "+this.echo(this.dataPathProps))}
this.saveFormValues()}
,isc.A.saveFormValues=function(){for(var i=0;i<this.managedProperties.length;i++){var _2=this.managedProperties[i],_3=this.dataPathProps[_2];this.form.setValue(_2,_3)}
if(this.form.saveProperties){this.form.saveProperties(this.dataPathProps,this.form.currentComponent)}}
,isc.A.clearFormValues=function(){for(var i=0;i<this.managedProperties.length;i++){this.form.setValue(this.managedProperties[i],null)}
if(this.form.saveProperties){this.form.saveProperties(this.dataPathProps,this.form.currentComponent)}}
);isc.B._maxIndex=isc.C+9;isc._moduleEnd=isc._Forms_end=(isc.timestamp?isc.timestamp():new Date().getTime());if(isc.Log&&isc.Log.logIsInfoEnabled('loadTime'))isc.Log.logInfo('Forms module init time: ' + (isc._moduleEnd-isc._moduleStart) + 'ms','loadTime');}else{if(window.isc && isc.Log && isc.Log.logWarn)isc.Log.logWarn("Duplicate load of module 'Forms'.");}
/*
 * Isomorphic SmartClient
 * Version 8.0 (2010-03-03)
 * Copyright(c) 1998 and beyond Isomorphic Software, Inc. All rights reserved.
 * "SmartClient" is a trademark of Isomorphic Software, Inc.
 *
 * licensing@smartclient.com
 *
 * http://smartclient.com/license
 */


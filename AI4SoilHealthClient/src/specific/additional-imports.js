/**
 * @desc Module for adding application specific styles, icons and imports
 * @module additionalImports
 */
import 'ol/ol.css'

// additional icons
import { symOutlinedDragPan, symOutlinedArrowSelectorTool, symOutlinedMyLocation, symOutlinedCircle, symOutlinedSquare }
from '@quasar/extras/material-symbols-outlined'
let icons = {
  drag_pan: symOutlinedDragPan,
  arrow_selector_tool: symOutlinedArrowSelectorTool,
  my_location: symOutlinedMyLocation,
  circle: symOutlinedCircle,
  square: symOutlinedSquare
}
export { icons }
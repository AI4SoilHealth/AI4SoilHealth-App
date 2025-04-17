<template>
    <!--  -->
    <div>
        <Header
            v-if="!parentPopup"
            ref="header"
            :back-button="backButton"
            :name="$route.name"
            :title="`${title ?? $t($route.name)}&nbsp&nbsp·&nbsp&nbsp${reportName}`"
        />
        <!-- <div :style="containerStyle"></div> -->
        <div>
            <div class="editing-toolbar" :style="editingToolbarStyle">
                <div class="left-item row">
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="zoom_out"
                            @click="decreaseZoomLevel"
                        >
                            <q-tooltip>{{ $t('Zoom out') }}</q-tooltip>
                        </q-btn>
                        <q-input
                            v-model="zoomPercentageInput"
                            class="zoom-percentage-input __editing_toolbar_input__q_specifier_for_unscoped_styles__"
                            input-class="editing-toolbar-input-field"
                            @keyup.enter="setCustomZoom"
                        />
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="zoom_in"
                            @click="increaseZoomLevel"
                        >
                            <q-tooltip>{{ $t('Zoom in') }}</q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="preview"
                            @click="showPrintPreview"
                        >
                            <q-tooltip>{{ $t('Print Preview') }}</q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="download"
                            @click="downloadReport"
                        >
                            <q-tooltip>{{ $t('Download') }}</q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="print"
                            @click="printReport"
                        >
                            <q-tooltip>{{ $t('Print') }}</q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="positive"
                            dense
                            :disable="!$store.formChanged"
                            flat
                            icon="save"
                            @click="saveReport"
                        >
                            <q-tooltip>{{ $t('Save report') }}</q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="negative"
                            dense
                            :disable="!$store.formChanged"
                            flat
                            icon="undo"
                            @click="undoChanges"
                        >
                            <q-tooltip>{{ $t('Undo changes') }} </q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row">
                        <q-btn
                            color="primary"
                            dense
                            flat
                            icon="restart_alt"
                            @click="reload"
                        >
                            <q-tooltip>
                                {{ $t('Reload') }}
                            </q-tooltip>
                        </q-btn>
                    </div>
                    <div class="editing-toolbar-group row"></div>
                    <div
                        class="editing-toolbar-group row section-editing-toolbar"
                    >
                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                flat
                                icon="article"
                                @click="showSectionsSelect"
                            >
                                <q-select
                                    ref="selectSections"
                                    v-model="shownSections"
                                    class="sections-select"
                                    dense
                                    hide-dropdown-icon
                                    hide-selected
                                    multiple
                                    :options="
                                        Object.entries(
                                            defaultState.sections,
                                        ).map(([sectionId, section]) => ({
                                            label: section.text,
                                            value: sectionId,
                                        }))
                                    "
                                >
                                    <template
                                        #option="{
                                            itemProps,
                                            opt,
                                            label,
                                            selected,
                                            toggleOption,
                                        }"
                                    >
                                        <q-item v-bind="itemProps">
                                            <q-item-section>
                                                <q-item-label>
                                                    {{ label }}
                                                </q-item-label>
                                            </q-item-section>
                                            <q-item-section side>
                                                <!-- 
                                                 v-model="selected" -->
                                                <!--  -->
                                                <q-checkbox
                                                    :model-value="selected"
                                                    @update:model-value="
                                                        toggleOption(opt)
                                                    "
                                                />
                                            </q-item-section>
                                        </q-item>
                                    </template>
                                </q-select>
                                <q-tooltip>
                                    {{ $t('Sections') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                :disable="!isSectionSelected"
                                flat
                                icon="add"
                            >
                                <q-menu
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <q-list>
                                        <q-item
                                            v-close-popup
                                            clickable
                                            @click="addText"
                                        >
                                            <q-item-section>
                                                {{ $t('Add text') }}
                                            </q-item-section>
                                        </q-item>
                                        <q-item
                                            v-close-popup
                                            clickable
                                            @click="addImage"
                                        >
                                            <q-item-section>
                                                {{ $t('Add image') }}
                                            </q-item-section>
                                        </q-item>
                                        <q-item
                                            v-close-popup
                                            clickable
                                            :disable="!isPageSection"
                                            @click="addPageNumber"
                                        >
                                            <q-item-section>
                                                {{ $t('Add page number') }}
                                            </q-item-section>

                                            <q-tooltip
                                                v-if="!isPageSection"
                                                class="z-max-30000"
                                            >
                                                {{
                                                    $t(
                                                        'Page number can be added to page header or footer',
                                                    )
                                                }}
                                            </q-tooltip>
                                        </q-item>
                                    </q-list>
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Add element') }}
                                </q-tooltip>
                            </q-btn>
                        </div>
                        <div
                            class="editing-toolbar-group row element-editing-toolbar"
                        >
                            <q-btn
                                :color="
                                    isElementSelected || isSectionSelected
                                        ? 'red'
                                        : 'primary'
                                "
                                dense
                                :disable="
                                    !isElementSelected && !isSectionSelected
                                "
                                flat
                                icon="delete"
                                @click="deleteSelectedElements"
                            >
                                <q-tooltip>
                                    {{
                                        isElementSelected
                                            ? $t('Delete element')
                                            : $t('Delete elements')
                                    }}
                                </q-tooltip>
                            </q-btn>
                        </div>
                    </div>
                    <div
                        class="editing-toolbar-group row element-editing-toolbar"
                    >
                        <q-select
                            ref="selectFontFamily"
                            v-model="fontFamily"
                            class="font-family-input __editing_toolbar_input__q_specifier_for_unscoped_styles__"
                            dense
                            :disable="!isTextElementSelected"
                            fill-input
                            filled
                            input-class="editing-toolbar-input-field font-family-input-field"
                            :options="filteredFontFamilies"
                            @focus="focusFontFamily"
                            @popup-show="startFontFamilyEditing"
                        >
                            <template #prepend>
                                <!-- TODO: check why serif does not work -->
                                <!-- <q-icon name="serif" /> -->
                                <q-icon color="primary" name="title" />
                            </template>
                            <template #before-options>
                                <q-icon name="search" />
                                <input
                                    ref="inputFontFamilyFilter"
                                    v-model="fontFamilyFilter"
                                >
                            </template>
                        </q-select>
                        <q-select
                            v-model="fontSize"
                            class="font-size-input __editing_toolbar_input__q_specifier_for_unscoped_styles__"
                            dense
                            :disable="!isTextElementSelected"
                            fill-input
                            filled
                            hide-selected
                            input-class="editing-toolbar-input-field font-size-input-field"
                            option-disable
                            :options="fontSizes"
                            use-input
                            @input-value="fontSizeInputChanged"
                            @keyup.enter.stop="setCustomFontSize"
                        >
                            <template #prepend>
                                <q-icon color="primary" name="format_size" />
                            </template>
                        </q-select>
                        <q-toggle
                            v-model="fontBold"
                            color="primary"
                            :disable="!isTextElementSelected"
                            icon="format_bold"
                        >
                            <q-tooltip>{{ $t('Bold') }}</q-tooltip>
                        </q-toggle>
                        <q-toggle
                            v-model="fontItalic"
                            color="primary"
                            :disable="!isTextElementSelected"
                            icon="format_italic"
                        >
                            <q-tooltip>{{ $t('Italic') }}</q-tooltip>
                        </q-toggle>
                        <q-toggle
                            v-model="fontUnderline"
                            color="primary"
                            :disable="!isTextElementSelected"
                            icon="format_underline"
                        >
                            <q-tooltip>{{ $t('Underline') }}</q-tooltip>
                        </q-toggle>
                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                :disable="!isTextElementSelected"
                                flat
                                icon="subject"
                            >
                                <q-menu
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <div class="row no-wrap">
                                        <q-item
                                            :active="textAlign === 'left'"
                                            active-class="align-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="format_align_left"
                                                @click="alignText('left')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Align left') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="textAlign === 'center'"
                                            active-class="align-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="format_align_center"
                                                @click="alignText('center')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Center') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="textAlign === 'right'"
                                            active-class="align-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="format_align_right"
                                                @click="alignText('right')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Align right') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="textAlign === 'justify'"
                                            active-class="align-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="format_align_justify"
                                                @click="alignText('justify')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Justify') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                    </div>
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Align text') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                :disable="!isTextElementSelected"
                                flat
                                icon="wrap_text"
                            >
                                <q-menu
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <div class="row no-wrap">
                                        <q-item
                                            :active="textWrap === 'overflow'"
                                            active-class="wrap-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                :icon="
                                                    icons.format_text_overflow
                                                "
                                                @click="wrapText('overflow')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Overflow') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="textWrap === 'wrap'"
                                            active-class="wrap-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                :icon="icons.format_text_wrap"
                                                @click="wrapText('wrap')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Wrap') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="textWrap === 'clip'"
                                            active-class="wrap-text-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                :icon="icons.format_text_clip"
                                                @click="wrapText('clip')"
                                            >
                                                <q-tooltip>
                                                    {{ $t('Clip') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                    </div>
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Wrap text') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                :disable="!isTextElementSelected"
                                flat
                                icon="format_color_text"
                            >
                                <q-menu
                                    :style="{
                                        width: '250px',
                                    }"
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <q-toggle
                                        v-model="expandTextColorRules"
                                        color="primary"
                                        icon="rule"
                                        label="Color rules"
                                    />
                                    <q-expansion-item
                                        v-model="expandTextColorRules"
                                        header-class="hidden"
                                        :style="{
                                            'max-width': '100%',
                                        }"
                                    >
                                        <div
                                            v-for="rule in state.elements[
                                                selectedElementId
                                            ].styleData.textColorRules"
                                            :key="rule"
                                            class="row justify-center"
                                        >
                                            <q-input
                                                v-model="rule.function"
                                                class="col-9"
                                                dense
                                                label="Function"
                                            >
                                                <template #prepend>
                                                    <q-icon
                                                        :name="icons.function"
                                                    />
                                                </template>
                                            </q-input>
                                            <!-- color="red" -->
                                            <q-btn
                                                class="col-2"
                                                dense
                                                flat
                                                icon="palette"
                                                :style="{
                                                    color: rule.color,
                                                }"
                                                @click="
                                                    chooseTextRuleColor(rule)
                                                "
                                            />
                                        </div>

                                        <div class="row justify-center">
                                            <q-input
                                                v-model="
                                                    textColorNewRuleFunction
                                                "
                                                class="col-9"
                                                dense
                                                label="Function"
                                            >
                                                <template #prepend>
                                                    <q-icon
                                                        :name="icons.function"
                                                    />
                                                </template>
                                                <template #append>
                                                    <q-icon
                                                        name="info"
                                                        size="xs"
                                                    >
                                                        <q-tooltip
                                                            class="z-max-30000 color-rule-tooltip __editing_toolbar__q_specifier_for_unscoped_styles__"
                                                            color="primary"
                                                        >
                                                            <div
                                                                :style="{
                                                                    maxWidth:
                                                                        '300px',
                                                                }"
                                                            >
                                                                <div>
                                                                    Use
                                                                    JavaScript
                                                                    code to
                                                                    create rule
                                                                    with
                                                                    arguments
                                                                    value, and
                                                                    rowIndex.
                                                                </div>
                                                                <div>
                                                                    Examples:
                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value > 0,</pre>

                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value.includes('test'),</pre>

                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value === 'test' && rowIndex % 2 === 0.</pre>
                                                                </div>
                                                            </div>
                                                        </q-tooltip>
                                                    </q-icon>
                                                </template>
                                            </q-input>
                                            <q-btn
                                                class="col-2"
                                                color="primary"
                                                dense
                                                :disable="
                                                    textColorNewRuleFunction ===
                                                        ''
                                                "
                                                flat
                                                icon="palette"
                                                @click="
                                                    chooseTextRuleColor(null)
                                                "
                                            />
                                        </div>
                                        <div class="row justify-center">
                                            <q-field class="col-9" dense>
                                                <template #prepend>
                                                    <q-icon />
                                                </template>
                                                <template #control>
                                                    <div
                                                        class="self-center full-width no-outline"
                                                        tabindex="0"
                                                    >
                                                        {{
                                                            $t('Default color')
                                                        }}
                                                    </div>
                                                </template>
                                            </q-field>
                                            <q-btn
                                                class="col-2"
                                                dense
                                                flat
                                                icon="palette"
                                                :style="{
                                                    color: textColor,
                                                }"
                                                @click="chooseTextDefaultColor"
                                            />
                                        </div>
                                    </q-expansion-item>

                                    <q-color
                                        v-model="textColor"
                                        default-view="palette"
                                        :disable="
                                            expandTextColorRules &&
                                                !textColorChooseColor
                                        "
                                    />
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Text color') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div
                            class="editing-toolbar-group row section-editing-toolbar"
                        >
                            <q-btn
                                color="primary"
                                dense
                                :disable="
                                    !isElementSelected && !isSectionSelected
                                "
                                flat
                                icon="format_color_fill"
                            >
                                <q-menu
                                    :style="{
                                        width: '250px',
                                    }"
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <q-toggle
                                        v-model="expandFillColorRules"
                                        color="primary"
                                        icon="rule"
                                        label="Color rules"
                                    />
                                    <q-expansion-item
                                        v-model="expandFillColorRules"
                                        header-class="hidden"
                                        :style="{
                                            'max-width': '100%',
                                        }"
                                    >
                                        <div
                                            v-for="rule in fillColorRules"
                                            :key="rule"
                                            class="row justify-center"
                                        >
                                            <q-input
                                                v-model="rule.function"
                                                class="col-9"
                                                dense
                                                label="Function"
                                            >
                                                <template #prepend>
                                                    <q-icon
                                                        :name="icons.function"
                                                    />
                                                </template>
                                            </q-input>
                                            <!-- color="red" -->
                                            <q-btn
                                                class="col-2"
                                                dense
                                                flat
                                                icon="palette"
                                                :style="{
                                                    color: rule.color,
                                                }"
                                                @click="
                                                    chooseFillRuleColor(rule)
                                                "
                                            />
                                        </div>

                                        <div class="row justify-center">
                                            <q-input
                                                v-model="
                                                    fillColorNewRuleFunction
                                                "
                                                class="col-9"
                                                dense
                                                label="Function"
                                            >
                                                <template #prepend>
                                                    <q-icon
                                                        :name="icons.function"
                                                    />
                                                </template>
                                                <template #append>
                                                    <q-icon
                                                        name="info"
                                                        size="xs"
                                                    >
                                                        <q-tooltip
                                                            class="z-max-30000 color-rule-tooltip __editing_toolbar__q_specifier_for_unscoped_styles__"
                                                            color="primary"
                                                        >
                                                            <div
                                                                v-if="
                                                                    isElementSelected
                                                                "
                                                                :style="{
                                                                    maxWidth:
                                                                        '300px',
                                                                }"
                                                            >
                                                                <div>
                                                                    Use
                                                                    JavaScript
                                                                    code to
                                                                    create rule
                                                                    with
                                                                    arguments
                                                                    value, and
                                                                    rowIndex.
                                                                </div>
                                                                <div>
                                                                    Examples:
                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value > 0,</pre>

                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value.includes('test'),</pre>

                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
value === 'test' && rowIndex % 2 === 0.</pre>
                                                                </div>
                                                            </div>
                                                            <div
                                                                v-if="
                                                                    isSectionSelected
                                                                "
                                                                :style="{
                                                                    maxWidth:
                                                                        '300px',
                                                                }"
                                                            >
                                                                <div>
                                                                    Use
                                                                    JavaScript
                                                                    code to
                                                                    create rule
                                                                    with
                                                                    argument
                                                                    rowIndex.
                                                                </div>
                                                                <div>
                                                                    Example:
                                                                    <pre
                                                                        :style="{
                                                                            margin: 0,
                                                                        }"
                                                                    >
rowIndex % 2 === 0.</pre>
                                                                </div>
                                                            </div>
                                                        </q-tooltip>
                                                    </q-icon>
                                                </template>
                                            </q-input>
                                            <q-btn
                                                class="col-2"
                                                color="primary"
                                                dense
                                                :disable="
                                                    fillColorNewRuleFunction ===
                                                        ''
                                                "
                                                flat
                                                icon="palette"
                                                @click="
                                                    chooseFillRuleColor(null)
                                                "
                                            />
                                        </div>
                                        <div class="row justify-center">
                                            <q-field class="col-9" dense>
                                                <template #prepend>
                                                    <q-icon />
                                                </template>
                                                <template #control>
                                                    <div
                                                        class="self-center full-width no-outline"
                                                        tabindex="0"
                                                    >
                                                        {{
                                                            $t('Default color')
                                                        }}
                                                    </div>
                                                </template>
                                            </q-field>
                                            <q-btn
                                                class="col-2"
                                                dense
                                                flat
                                                icon="palette"
                                                :style="{
                                                    color: fillColor,
                                                }"
                                                @click="chooseFillDefaultColor"
                                            />
                                        </div>
                                    </q-expansion-item>

                                    <q-color
                                        v-model="fillColor"
                                        default-view="palette"
                                        :disable="
                                            expandFillColorRules &&
                                                !fillColorChooseColor
                                        "
                                        format-model="hexa"
                                    />
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Fill color') }}
                                </q-tooltip>
                            </q-btn>
                        </div>
                    </div>
                    <div class="editing-toolbar-group row page-editing-toolbar">
                        <div class="editing-toolbar-group row">
                            <q-btn color="primary" dense flat icon="margin">
                                <q-menu
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <q-list>
                                        <!--  -->
                                        <q-item
                                            v-for="(
                                                marginsInCm, marginsName
                                            ) in marginValuesInCm"
                                            :key="marginsName"
                                            v-close-popup
                                            :active="
                                                areMarginsActive(marginsInCm)
                                            "
                                            active-class="adjust-margins-item"
                                            clickable
                                            @click="adjustMargins(marginsName)"
                                        >
                                            <q-item-section>
                                                {{ $t(marginsName) }}
                                            </q-item-section>
                                        </q-item>
                                        <q-item
                                            v-close-popup
                                            active-class="adjust-margins-item"
                                            clickable
                                            @click="adjustMargins('custom')"
                                        >
                                            <q-item-section>
                                                {{ $t('Custom') }}
                                            </q-item-section>
                                        </q-item>
                                        <!-- clickable -->
                                        <q-item
                                            v-close-popup
                                            class="margins-visibility-item"
                                        >
                                            <q-toggle
                                                v-model="state.areMarginsShown"
                                                checked-icon="visibility"
                                                color="primary"
                                                icon="visibility"
                                                unchecked-icon="visibility_off"
                                            >
                                                <q-tooltip>
                                                    {{
                                                        state.areMarginsShown
                                                            ? $t('Hide margins')
                                                            : $t('Show margins')
                                                    }}
                                                </q-tooltip>
                                            </q-toggle>
                                        </q-item>
                                    </q-list>
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Adjust margins') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div class="editing-toolbar-group row">
                            <q-btn
                                color="primary"
                                dense
                                flat
                                icon="restore_page"
                            >
                                <q-menu
                                    @before-show="onMenuOpen"
                                    @hide="onCloseMenu"
                                >
                                    <div class="row no-wrap">
                                        <q-item
                                            :active="
                                                state.pageOrientation ===
                                                    'portrait'
                                            "
                                            active-class="page-orientation-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="
                                                    crop_portrait
                                                "
                                                @click="
                                                    setPageOrientation(
                                                        'portrait',
                                                    )
                                                "
                                            >
                                                <q-tooltip>
                                                    {{ $t('Portrait') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                        <q-item
                                            :active="
                                                state.pageOrientation ===
                                                    'landscape'
                                            "
                                            active-class="page-orientation-item"
                                            class="column menu-button-container"
                                        >
                                            <q-btn
                                                v-close-popup
                                                color="primary"
                                                dense
                                                flat
                                                icon="crop_landscape"
                                                @click="
                                                    setPageOrientation(
                                                        'landscape',
                                                    )
                                                "
                                            >
                                                <q-tooltip>
                                                    {{ $t('Landscape') }}
                                                </q-tooltip>
                                            </q-btn>
                                        </q-item>
                                    </div>
                                </q-menu>
                                <q-tooltip>
                                    {{ $t('Page orientation') }}
                                </q-tooltip>
                            </q-btn>
                        </div>

                        <div
                            v-if="contextValuesName"
                            class="editing-toolbar-group row"
                        >
                            <q-checkbox v-model="contextValuesDependent">
                                <q-tooltip>
                                    {{ $t('Context values dependent') }}
                                </q-tooltip>
                            </q-checkbox>
                        </div>
                    </div>
                    <q-resize-observer @resize="onEditingToolbarResize" />
                </div>
            </div>
            <q-card class="editing-q-card">
                <editing-container
                    v-if="isStateSet"
                    v-model:zoom="state.zoom"
                    class="editing-container bg-blue-grey-1"
                    :container-height="containerHeight"
                    :container-width="containerWidth"
                    :decrease-zoom-number="decreaseZoomNumber"
                    :increase-zoom-number="increaseZoomNumber"
                    :maximum-page-border-width="1"
                    :page-size="state.pageSize"
                    :reset-zoom-number="resetZoomNumber"
                    :style-rounding-precision="styleRoundingPrecision"
                >
                    <page-container
                        :height="state.pageSize.height"
                        :page-border-width="1"
                        :page-margins="marginsInPx"
                        :page-margins-color="marginsColor"
                        :style-rounding-precision="styleRoundingPrecision"
                        :width="state.pageSize.width"
                    >
                        <!--
                        editing-toolbar-selector=".section-editing-toolbar, .q-menu, .q-dialog"
                        TODO: can not use .q-dialog because editor component
                        may be opened in a dialog, but add image also uses dialog,
                        use more specific selector
                    -->
                        <interactable-element
                            v-for="(section, sectionId) in state?.sections"
                            :key="sectionId"
                            v-model:height="section.layout.height"
                            v-model:selected="
                                editingState.sections[sectionId].isSelected
                            "
                            v-model:top="section.layout.top"
                            v-model:width="section.layout.width"
                            class="section"
                            :container-size="pageContainerSize"
                            :content-style="getSectionStyle(sectionId)"
                            draggable
                            editing-toolbar-selector=".section-editing-toolbar, .q-menu"
                            :emit-changes="['layout-update']"
                            :recalculate-layout-number="recalculateLayoutNumber"
                            resizable
                            :resize-handlers="['t', 'b']"
                            :style="sectionStyle"
                            :style-rounding-precision="styleRoundingPrecision"
                            @layout-update="sectionLayoutChanged"
                        >
                            <div class="section-label">
                                {{ section.text }}
                            </div>
                            <!-- 
                            :container-size="sectionsSizes[sectionId]"
                            :container-size="getInnerSectionSize(section.layout)" 
                        -->
                            <!-- 
                            @selection-update="
                                (isSelected) =>
                                    setSelectedElement(elementId, isSelected)
                            "
                            @selection-update="
                                (isSelected) =>
                                    onElementSelected(elementId, isSelected)
                            "
                        -->
                            <interactable-element
                                v-for="elementId in section.elements"
                                :key="elementId"
                                v-model:height="
                                    state.elements[elementId].layout.height
                                "
                                v-model:left="
                                    state.elements[elementId].layout.left
                                "
                                v-model:selected="
                                    editingState.elements[elementId].isSelected
                                "
                                v-model:top="
                                    state.elements[elementId].layout.top
                                "
                                v-model:width="
                                    state.elements[elementId].layout.width
                                "
                                class="editing-element __editing_element__q_specifier_for_unscoped_styles__"
                                :class="{
                                    'editing-element-highlighted':
                                        editingState.elements[elementId]
                                            .isSelected,
                                }"
                                :container-size="
                                    getInnerSectionSize(
                                        sectionId,
                                        section.layout.width,
                                        section.layout.height,
                                    )
                                "
                                :content-style="getElementStyle(elementId)"
                                draggable
                                editing-toolbar-selector=".element-editing-toolbar, .q-menu"
                                :emit-changes="[
                                    'layout-update',
                                    'resize-update',
                                ]"
                                :fit-content="
                                    state.elements[elementId].layout
                                        ?.fitContent ?? false
                                "
                                :force-inside-container="
                                    forceElementsInsideContainer?.[elementId] ??
                                        true
                                "
                                :grid="elementWidthRelativeGridSizes"
                                :justify-content="
                                    state.elements[elementId].styleData
                                        .textAlign === 'justify'
                                        ? 'left'
                                        : state.elements[elementId].styleData
                                            .textAlign
                                "
                                :keep-ratio="
                                    state.elements[elementId].type ===
                                        'image' && isShiftPressed
                                "
                                :recalculate-layout-number="
                                    recalculateLayoutNumber
                                "
                                resizable
                                :style-rounding-precision="
                                    styleRoundingPrecision
                                "
                                :wait-content="
                                    waitElementsContents?.[elementId] ?? false
                                "
                                @layout-update="elementLayoutChanged"
                                @resize-update="
                                    (size) => onElementResize(elementId, size)
                                "
                            >
                                <q-input
                                    v-if="
                                        state.elements[elementId].type ===
                                            'text'
                                    "
                                    :ref="'elementInput' + elementId"
                                    v-model="state.elements[elementId].text"
                                    borderless
                                    :class="{
                                        __input__q_specifier_for_unscoped_styles__: true,
                                        __input_fit_content__q_specifier_for_unscoped_styles__:
                                            state.elements[elementId].layout
                                                ?.fitContent ?? false,
                                    }"
                                    dense
                                    :input-style="
                                        getElementTextStyle(elementId)
                                    "
                                    square
                                    @update:model-value="
                                        (value) =>
                                            elementInputChanged(
                                                value,
                                                elementId,
                                            )
                                    "
                                />
                                <q-img
                                    v-if="
                                        state.elements[elementId].type ===
                                            'image'
                                    "
                                    fit="fill"
                                    ratio
                                    :src="state.elements[elementId].imageUrl"
                                    @load="onImageLoad(elementId)"
                                />
                                <q-field
                                    v-if="
                                        state.elements[elementId].type ===
                                            'page_number'
                                    "
                                    borderless
                                    class="__field__q_specifier_for_unscoped_styles__"
                                    dense
                                    square
                                    :style="getElementTextStyle(elementId)"
                                >
                                    <div>
                                        {{ state.elements[elementId].text }}
                                    </div>
                                </q-field>
                            </interactable-element>
                        </interactable-element>
                    </page-container>
                </editing-container>
            </q-card>

            <q-dialog v-model="inAddingImage" persistent>
                <q-card>
                    <q-card-section>
                        <!-- dense -->
                        <q-input
                            v-model="addingImageUrl"
                            class="add-image-url-input"
                            :label="$t('Image URL')"
                        />
                    </q-card-section>
                    <q-card-actions align="right">
                        <q-btn
                            color="positive"
                            dense
                            flat
                            :label="$t('OK')"
                            no-caps
                            @click="onAddingImageOKClick"
                        />
                        <q-btn
                            color="negative"
                            dense
                            flat
                            :label="$t('Cancel')"
                            no-caps
                            @click="onAddingImageCancelClick"
                        />
                    </q-card-actions>
                </q-card>
            </q-dialog>
        </div>
    </div>
</template>

<script>
import {
    PdfReportMixin,
    validatePdfReportState,
    DefaultStyleRoundingPrecision,
    DefaultInternalRoundingPrecision,
    validateRoundingPrecision,
    DefaultPdfReportState,
    PdfUtilsMixin,
} from '../mixins/pdf.js';

import { loadComponent } from '@/common/component-loader';

import {
    symOutlinedFormatTextOverflow,
    symOutlinedFormatTextWrap,
    symOutlinedFormatTextClip,
    symOutlinedFunction,
} from '@quasar/extras/material-symbols-outlined';

// const DEBUG = false;
const DEBUG = true;

const propertyValidators = {
    title(value) {
        return typeof value === 'string';
    },
    backButton(value) {
        return typeof value === 'boolean';
    },
    top(value) {
        if (typeof value !== 'number') {
            return false;
        }
        return Number.isFinite(value);
    },
    fontSizes(value) {
        if (!Array.isArray(value)) {
            return false;
        }
        return value.every(
            (size, index) =>
                typeof size === 'number' &&
                size > 0 &&
                (index === 0 || size >= value[index - 1]),
        );
    },
    fontFamilies(value) {
        if (!Array.isArray(value)) {
            return false;
        }
        return value.every((family) => typeof family === 'string');
    },
    styleRoundingPrecision: validateRoundingPrecision,
    internalRoundingPrecision: validateRoundingPrecision,
    state: validatePdfReportState,
    data(value) {
        if (typeof value !== 'object') {
            return false;
        }
        if (
            !['columns', 'rows'].every((key) => typeof value?.[key] == 'object')
        ) {
            return false;
        }
        if (
            'columnWidths' in value &&
            !(
                Array.isArray(value.columnWidths) &&
                value.columnWidths.every((width) => typeof width === 'number')
            )
        ) {
            return false;
        }
        return true;
    },
};

export default {
    name: 'PdfReportEditor',
    components: {
        EditingContainer: loadComponent('editing-container'),
        PageContainer: loadComponent('page-container'),
        // InteractableSection: loadComponent('interactable-section'),
        InteractableElement: loadComponent('interactable-element'),
    },
    mixins: [PdfReportMixin, PdfUtilsMixin],
    beforeRouteEnter(to, from, next) {
        next((vm) => {
            vm.init(to.name);
        });
    },
    beforeRouteUpdate(to, from, next) {
        this.init(to.name);
        next();
    },
    props: {
        parentPopup: {
            type: Object,
            default: null,
        },
        propTitle: {
            type: String,
            default: null,
            validator: propertyValidators.title,
        },
        propBackButton: {
            type: Boolean,
            validator: propertyValidators.backButton,
        },
        propApiUrl: {
            type: String,
            default: null,
        },
        propApiOptions: {
            type: Object,
            default: () => ({}),
        },
        propReportName: {
            type: String,
            default: null,
        },
        propTableName: {
            type: String,
            default: null,
        },
        propContextValuesName: {
            type: String,
            default: null,
        },
        propFontFamilies: {
            type: Array,
            default: () => ['Arial', 'Times New Roman', 'Courier New'],
            validator: propertyValidators.fontFamilies,
        },
        propFontSizes: {
            type: Array,
            default: () => [
                6, 8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72,
            ],
            validator: propertyValidators.fontSizes,
        },
        propTop: {
            type: Number,
            default: 0,
            validator: propertyValidators.top,
        },
        propStyleRoundingPrecision: {
            type: Number,
            default: DefaultStyleRoundingPrecision,
            validator: propertyValidators.styleRoundingPrecision,
        },
        propInternalRoundingPrecision: {
            type: Number,
            default: DefaultInternalRoundingPrecision,
            validator: propertyValidators.internalRoundingPrecision,
        },
        propState: {
            type: Object,
            default: null,
            validator: propertyValidators.state,
        },
        propData: {
            type: Object,
            default: null,
            validator: propertyValidators.data,
        },
    },
    data() {
        return {
            // TODO: fix order of data properties
            pdfTableApi: 'general_pdf_template',
            title: this.propTitle,
            backButton: this.propBackButton,
            // TODO: add option for api and apiOptions to be props
            apiUrl: this.propApiUrl,
            apiOptions: this.propApiOptions,
            reportName: this.propReportName,
            tableName: this.propTableName,
            contextValuesName: this.propContextValuesName,
            row: null,
            savedSelectionData: null,
            menuOpenedCounter: 0,
            menuClosedCounter: 0,
            shownSections: [],
            fontFamilies: this.propFontFamilies,
            defaultFontFamily: null,
            fontFamily: null,
            fontFamilyFilter: '',
            fontSizes: this.propFontSizes,
            defaultFontSize: null,
            fontSize: null,
            fontSizeCurrentInput: null,
            fontBold: false,
            fontItalic: false,
            fontUnderline: false,
            textColor: null,
            defaultTextColor: '#000000ff',
            expandTextColorRules: false,
            textColorNewRuleFunction: '',
            textColorChooseRuleColor: false,
            textColorChooseDefaultColor: false,
            textColorCurrentRule: null,
            fillColor: null,
            defaultFillColor: '#01010100',
            expandFillColorRules: false,
            fillColorNewRuleFunction: '',
            fillColorChooseRuleColor: false,
            fillColorChooseDefaultColor: false,
            fillColorCurrentRule: null,
            contextValuesDependent: true,
            styleRoundingPrecision: this.propStyleRoundingPrecision,
            internalRoundingPrecision: this.propInternalRoundingPrecision,
            recalculateLayoutNumber: 0,
            isShiftPressed: false,
            forceElementsInsideContainer: {},
            waitElementsContents: {},
            defaultState: DefaultPdfReportState,
            state: this.propState,
            // TODO: use this to set fromChanged to false
            // if option is returned to the last saved state
            // like for areMarginsShown
            lastSavedState: null,
            editingState: null,
            top: this.propTop,
            data: this.propData,
            headerHeight: 0,
            pageMarginsColor: 'rgb(from lightgrey r g b / 20%)',
            sectionBorderWidth: 0.5,
            inAddingImage: false,
            addingImageUrl: '',
            // create a prop for this
            defaultImageWidth: 100,
            // TODO: remove this and optimize by something like lazy loading
            gridSizeInPx: 10,
            maximumDetailsPreviewRows: 1000,
            // TODO: consider moving this to additional-imports.js
            icons: {
                /* eslint-disable camelcase */
                format_text_overflow: symOutlinedFormatTextOverflow,
                format_text_wrap: symOutlinedFormatTextWrap,
                format_text_clip: symOutlinedFormatTextClip,
                function: symOutlinedFunction,
                /* eslint-enable camelcase */
            },
        };
    },
    computed: {
        editingToolbarStyle() {
            return {
                maxWidth: this.getCSSPixelValue(this.containerWidth),
            };
        },
        filteredFontFamilies() {
            const lowerCasedFilter = this.fontFamilyFilter.toLowerCase();
            const filteredOptions = this.fontFamilies.filter((option) =>
                option.toLowerCase().includes(lowerCasedFilter),
            );
            return filteredOptions.length === 0
                ? this.fontFamilies
                : filteredOptions;
        },
        textAlign() {
            const selectedElementStyleData =
                this.state.elements?.[this.selectedElementId]?.styleData;
            // TODO: check if null or undefined should be returned
            return selectedElementStyleData?.textAlign ?? null;
        },
        textWrap() {
            const selectedElementStyleData =
                this.state.elements?.[this.selectedElementId]?.styleData;
            // TODO: check if null or undefined should be returned
            return selectedElementStyleData?.textWrap ?? null;
        },
        textColorChooseColor() {
            return (
                this.textColorChooseDefaultColor ||
                this.textColorChooseRuleColor
            );
        },
        fillColorChooseColor() {
            return (
                this.fillColorChooseDefaultColor ||
                this.fillColorChooseRuleColor
            );
        },
        fillColorRules() {
            // if (!this.isStateSet) {
            //     return [];
            // }
            if (this.isElementSelected) {
                return this.state.elements[this.selectedElementId].styleData
                    .fillColorRules;
            } else if (this.isSectionSelected) {
                return this.state.sections[this.selectedSectionId].styleData
                    .fillColorRules;
            }
            return [];
        },
        selectedElementId() {
            if (!this.isStateSet) {
                // TODO: check if null or undefined should be returned
                return null;
            }
            for (const elementId in this.editingState.elements) {
                if (this.editingState.elements[elementId].isSelected) {
                    return elementId;
                }
            }
            return null;
        },
        isElementSelected() {
            return this.selectedElementId !== null;
        },
        isTextElementSelected() {
            return (
                this.isElementSelected &&
                this.state.elements[this.selectedElementId].type === 'text'
            );
        },
        selectedSectionId() {
            if (!this.isStateSet) {
                // TODO: check if null or undefined should be returned
                return null;
            }
            for (const sectionId in this.editingState.sections) {
                if (this.editingState.sections[sectionId].isSelected) {
                    return sectionId;
                }
            }
            return null;
        },
        isSectionSelected() {
            return this.selectedSectionId !== null;
        },
        isPageSection() {
            return (
                this.selectedSectionId === 'pageHeader' ||
                this.selectedSectionId === 'pageFooter'
            );
        },
        containerStyle() {
            // TODO: remove if not used
            return {
                height: this.getCSSPixelValue(this.containerHeight),
            };
        },
        marginsColor() {
            return this.state?.areMarginsShown
                ? this.pageMarginsColor
                : 'transparent';
        },
        sectionStyle() {
            return {
                borderWidth: this.getCSSPixelValue(this.sectionBorderWidth),
            };
        },
        sectionsSizes() {
            // TODO: check if somehow computed properties
            // inside v-for can be used efficiently
            const sizes = {};
            for (const [sectionId, section] of Object.entries(
                this.state.sections,
            )) {
                sizes[sectionId] = this.getInnerSectionSize(section.layout);
            }
            return sizes;
        },
        elementWidthRelativeGridSizes() {
            if (!this.isStateSet) {
                // TODO: check if null or undefined should be returned
                return null;
            }
            const relativeGridSize = this.roundTo(
                this.gridSizeInPx / this.pageContainerSize.width,
                4,
            );
            const gridOptions = { useRelativeUnits: true, relativeTo: 'width' };
            return [
                [relativeGridSize, gridOptions],
                [relativeGridSize, gridOptions],
            ];
        },
    },
    watch: {
        'state.zoom'(value) {
            if (value === null) {
                return;
            }
            this.resetZoomPercentageInput();
            this.contentChanged();
        },
        shownSections(value) {
            const sectionIds = value.map((option) => option.value);
            this.setSections(sectionIds);
        },
        fontSizes(value) {
            this.setDefaultFontSize(value);
        },
        fontSize(value) {
            this.onStylePropertyChange('fontSize', value);
        },
        fontFamilies(value) {
            this.setDefaultFontFamily(value);
        },
        fontFamily(value) {
            this.onStylePropertyChange('fontFamily', value);
        },
        fontBold(value) {
            this.onStylePropertyChange('fontWeight', value ? 'bold' : 'normal');
        },
        fontItalic(value) {
            this.onStylePropertyChange(
                'fontStyle',
                value ? 'italic' : 'normal',
            );
        },
        fontUnderline(value) {
            this.onStylePropertyChange(
                'textDecoration',
                value ? 'underline' : 'none',
            );
        },
        textColor(newValue, oldValue) {
            if (this.textColorChooseRuleColor) {
                if (this.textColorCurrentRule === null) {
                    const selectedElementStyleData =
                        this.state.elements?.[this.selectedElementId]
                            ?.styleData;
                    selectedElementStyleData.textColorRules.push({
                        function: this.textColorNewRuleFunction,
                        color: newValue,
                    });
                    this.textColorNewRuleFunction = '';
                } else {
                    this.textColorCurrentRule.color = newValue;
                    this.textColorCurrentRule = null;
                }
                this.textColorChooseRuleColor = false;
                this.textColor = oldValue;
                this.$store.formChanged = true;
                return;
            } else if (this.textColorChooseDefaultColor) {
                this.textColorChooseDefaultColor = false;
            }
            this.onStylePropertyChange('textColor', newValue);
        },
        expandTextColorRules(value) {
            if (value === false) {
                this.textColorNewRuleFunction = '';
                this.state.elements[
                    this.selectedElementId
                ].styleData.textColorRules = [];
                this.textColorChooseRuleColor = false;
                this.textColorChooseDefaultColor = false;
            }
        },
        fillColor(newValue, oldValue) {
            this.onFillColorChanged(newValue, oldValue);
        },
        expandFillColorRules(value) {
            if (value === false) {
                this.fillColorNewRuleFunction = '';

                this.fillColorChooseRuleColor = false;
                this.fillColorChooseDefaultColor = false;
                if (this.isElementSelected) {
                    this.state.elements[
                        this.selectedElementId
                    ].styleData.fillColorRules = [];
                } else if (this.isSectionSelected) {
                    this.state.sections[
                        this.selectedSectionId
                    ].styleData.fillColorRules = [];
                }
            }
        },
        'state.areMarginsShown'(value) {
            if (this.lastSavedState === null) {
                return;
            }
            if (value !== this.lastSavedState.areMarginsShown) {
                this.$store.formChanged = true;
            }
        },
        selectedElementId(value) {
            if (value === null) {
                this.fontBold = false;
                this.fontItalic = false;
                this.fontUnderline = false;
            } else if (this.state.elements[value].type === 'text') {
                this.fontSize =
                    this.state.elements[value].styleData.fontSize ??
                    this.defaultFontSize;
                this.fontFamily =
                    this.state.elements[value].styleData.fontFamily ??
                    this.defaultFontFamily;
                this.fontBold =
                    this.state.elements[value].styleData.fontWeight === 'bold';
                this.fontItalic =
                    this.state.elements[value].styleData.fontStyle === 'italic';
                this.fontUnderline =
                    this.state.elements[value].styleData.textDecoration ===
                    'underline';
                this.textColor =
                    this.state.elements[value].styleData.textColor ??
                    this.defaultTextColor;
                this.expandTextColorRules =
                    this.state.elements[value].styleData.textColorRules.length >
                    0;
                this.fillColor =
                    this.state.elements[value].styleData.fillColor ??
                    this.defaultFillColor;
                this.expandFillColorRules =
                    this.state.elements[value].styleData.fillColorRules.length >
                    0;
            }
        },
    },
    async mounted() {
        //
        if (this.parentPopup) {
            this.initializeComponent(this.parentPopup);
            for (const property of Object.keys(
                this.$store.newPopups[this.parentPopup.name].props,
            )) {
                this.validateProp(property);
            }
            const requestApi = 'Table/' + this.pdfTableApi;
            // TODO: optimize this get request(s)
            const pdfReportTemplates = await this.get(requestApi);
            if (pdfReportTemplates) {
                const nameIndexInData = pdfReportTemplates.attributes.findIndex(
                    (attribute) => attribute.name === 'name',
                );
                let savedRow;
                if (this.contextValuesName !== null) {
                    const name =
                        this.tableName + ' - ' + this.contextValuesName;
                    savedRow = pdfReportTemplates.data.find(
                        (row) => row[nameIndexInData] === name,
                    );
                }
                if (savedRow === undefined) {
                    savedRow = pdfReportTemplates.data.find(
                        (row) => row[nameIndexInData] === this.tableName,
                    );
                } else {
                    this.contextValuesDependent = true;
                }
                if (savedRow !== undefined) {
                    this.row = Object.fromEntries(
                        pdfReportTemplates.attributes.map(
                            (attribute, index) => [
                                attribute.name,
                                savedRow[index],
                            ],
                        ),
                    );
                    this.apiUrl = this.row.api;
                    this.apiOptions = JSON.parse(
                        this.row?.api_options ?? 'null',
                    );
                    this.reportName = this.row?.name;
                    const properties = ['apiUrl', 'apiOptions', 'name'];
                    const reportEditorState = JSON.parse(
                        this.row?.state ?? 'null',
                    );
                    if (
                        reportEditorState !== null &&
                        Object.keys(reportEditorState).length > 0
                    ) {
                        this.state = reportEditorState;
                        properties.push('state');
                    }
                    for (const property of properties) {
                        this.validateProp(property);
                    }
                }
            }
        }

        if (!this.parentPopup) {
            this.headerHeight = this.$refs.header.$el.offsetHeight;
        } else {
            this.headerHeight =
                this.$el.parentElement.parentElement.querySelector(
                    '.q-card__section.row',
                ).offsetHeight;
        }

        if (this.state === null) {
            await this.resetState();
        }
        this.lastSavedState = this.deepClone(this.state);

        this.setDefaultFontSize(this.fontSizes);
        this.fontSize = this.defaultFontSize;
        this.fontSizeCurrentInput = this.fontSize;
        this.setDefaultFontFamily(this.fontFamilies);
        this.fontFamily = this.defaultFontFamily;
        this.textColor = this.defaultTextColor;
        this.fillColor = this.defaultFillColor;

        let loadDataPromise;
        // TODO: split table rows data from state
        if (this.data !== null && this.row === null) {
            const attributes = this.data.columns;
            const rowsValues = this.data.isFrugal
                ? this.data.rows
                : this.data.rows.map((row) =>
                      attributes.map((attribute) => row[attribute]),
                  );
            loadDataPromise = this.loadData(
                attributes,
                rowsValues,
                this.data.columnWidths,
            );
        }
        this.resetEditingState();

        // TODO: this may be ok to skip if data was loaded with loadData()
        const setInputSizesPromise = this.setElementInputSizes();

        await Promise.all([loadDataPromise, setInputSizesPromise]);

        this.shownSections = Object.entries(this.state.sections).map(
            ([sectionId, section]) => ({
                label: section.text,
                value: sectionId,
            }),
        );

        let formChanged = false;
        if (this.state?.pageSize === undefined) {
            this.state.pageSize = this.defaultState.pageSize;
            formChanged = true;
        }
        this.resetZoomPercentageInput();
        this.$store.formChanged = formChanged;

        window.addEventListener('keydown', this.handleKeyDown);
        window.addEventListener('keyup', this.handleKeyUp);

        // this.resetData();
    },
    beforeUnmount() {
        // console.log('PdfReportEditor beforeDestroy');
        window.removeEventListener('keydown', this.handleKeyDown);
        window.removeEventListener('keyup', this.handleKeyUp);
    },
    // expose: [],
    methods: {
        // TODO: fix order of methods
        validateProp(propertyName) {
            if (!(propertyName in propertyValidators)) {
                return;
            }
            if (propertyValidators[propertyName](this[propertyName])) {
                return;
            }
            if (DEBUG) {
                console.error(
                    `Invalid prop: validator check failed for prop "${propertyName}"`,
                );
            }
        },
        async init(_routeName) {
            this.copyObject(
                this.$store.props[this.$route.path],
                this.$data,
                true,
            );

            const properties = Object.keys(this.$store.props[this.$route.path]);
            if (this.row !== undefined) {
                this.apiUrl = this.row.api;
                this.apiOptions = JSON.parse(this.row?.api_options ?? 'null');
                this.reportName = this.row?.name;

                properties.push('apiUrl', 'apiOptions', 'name');

                const reportEditorState = JSON.parse(this.row?.state ?? 'null');
                if (
                    reportEditorState !== null &&
                    Object.keys(reportEditorState).length > 0
                ) {
                    this.state = reportEditorState;
                    properties.push('state');
                }
            }

            for (const property of properties) {
                this.validateProp(property);
            }
        },
        async resetState() {
            this.state = this.deepClone(this.defaultState);
        },
        async resetEditingState() {
            this.editingState = {
                sections: {},
                elements: {},
            };
            for (const sectionId of Object.keys(this.state.sections)) {
                this.editingState.sections[sectionId] = {
                    isSelected: false,
                };
            }
            for (const elementId of Object.keys(this.state.elements)) {
                this.editingState.elements[elementId] = {
                    isSelected: false,
                };
            }
        },
        handleKeyDown(event) {
            if (event.key === 'Shift') {
                this.isShiftPressed = true;
            }
        },
        handleKeyUp(event) {
            if (event.key === 'Shift') {
                this.isShiftPressed = false;
            }
        },
        fontSizeInputChanged(value) {
            this.fontSizeCurrentInput = value;
        },
        setCustomFontSize() {
            this.fontSize = this.fontSizeCurrentInput;
        },
        focusFontFamily() {
            this.$refs.selectFontFamily.focus();
        },
        startFontFamilyEditing() {
            this.$nextTick(() => {
                this.fontFamilyFilter = '';
                this.$refs.inputFontFamilyFilter.focus();
            });
        },
        showPrintPreview() {
            this.initPopup ({
                component: 'pdf-report-preview',
                title: `PDF Report Preview\u00A0\u00A0·\u00A0\u00A0${this.reportName}`,
                state: this.state,
                styleRoundingPrecision: this.styleRoundingPrecision,
                canCloseIfFormChanged: true
            });
        },
        async saveReport() {
            let isSaved = false;
            const requestApi = 'Table/' + this.pdfTableApi;
            if (this.reportName === null) {
                this.reportName = this.tableName;
                if (
                    this.contextValuesName !== null &&
                    this.contextValuesDependent
                ) {
                    this.reportName += ' - ' + this.contextValuesName;
                }
            }
            if (this.row && this.row.name === this.reportName) {
                // if (this.row && 0) {
                // this.row.state = JSON.stringify(this.state);
                this.row.state = this.state;
                // const rowToSave = {...this.row};
                const rowToSave = {};
                this.copyObject(this.row, rowToSave, true);
                // this.prepareRow(rowToSave, this.columns);
                const result = await this.put(requestApi, rowToSave);
                isSaved = result !== null;
            } else {
                const rowData = {
                    name: this.reportName,
                    api: this.apiUrl,
                    // eslint-disable-next-line camelcase
                    api_options: this.apiOptions,
                    state: this.state,
                };
                const rowId = await this.post(requestApi, rowData);
                this.row = {
                    id: rowId,
                    ...rowData,
                };
            }
            if (isSaved) {
                this.lastSavedState = this.deepClone(this.state);
                this.$store.formChanged = false;
            }
        },
        undoChanges() {
            this.state = this.deepClone(this.lastSavedState);
            this.resetEditingState();
            this.$store.formChanged = false;
        },
        // resetData() {
        //     this.data = {
        //         documentHeader: [],
        //         pageHeader: [],
        //         details: [],
        //         pageFooter: [],
        //         documentFooter: [],
        //     };
        // },
        // snakeToSentence(s) {
        //     // TODO: consider using global snakeToSentence
        //     return s
        //         .replaceAll('_', ' ')
        //         .replace(/\b\w/, (l) => l.toUpperCase());
        // },
        getAttributesAndRows(apiData) {
            const isTableApi =
                this.apiUrl.startsWith('Table/') &&
                !this.apiUrl.startsWith('Table/GetTable');
            const isFrugal = isTableApi
                ? true
                : this.apiOptions?.frugal === 'true';
            const attributes = isFrugal
                ? apiData.attributes.map((attribute) => attribute.name)
                : Object.keys(apiData?.[0] ?? {});

            const filteredAttributeEntries = [];
            for (const [index, attribute] of attributes.entries()) {
                if (!attribute.endsWith('_id')) {
                    let attributeName = attribute;
                    const separatorPosition = attributeName.indexOf('__');
                    if (separatorPosition > 0) {
                        attributeName = attributeName.slice(
                            separatorPosition + 2,
                        );
                    }
                    if (attributeName.endsWith('_id_val')) {
                        attributeName = attributeName.slice(0, -7);
                    }
                    filteredAttributeEntries.push([index, attributeName]);
                }
            }
            const filteredAttributes = filteredAttributeEntries.map(
                ([_index, attributeName]) => attributeName,
            );

            // TODO: remove comment
            // const dataAttributes = isFrugal
            //     ? apiData.attributes.map((attribute) => attribute.name)
            //     : Object.keys(apiData?.[0] ?? {});
            // const rowsValues = isFrugal
            //     ? apiData.data
            //     : apiData.map((row) => Object.values(row));
            // const firstRowValues = isFrugal
            //     ? (apiData.data?.[0] ?? [])
            //     : Object.values(apiData?.[0] ?? {});
            // const firstRowValues = rowsValues[0];

            const rowsValues = isFrugal
                ? apiData.data
                : apiData.map((row) => Object.values(row));
            const filteredRowsValues = rowsValues.map((rowValues) =>
                filteredAttributeEntries.map(([index]) => rowValues[index]),
            );
            return [filteredAttributes, filteredRowsValues];
        },
        setSectionHeightToContent(sectionId) {
            const elements = this.state.sections[sectionId].elements.map(
                (elementId) => this.state.elements[elementId],
            );
            const minimumElementTop = Math.min(
                ...elements.map((element) => element.layout.top),
            );
            const maximumElementBottom = Math.max(
                ...elements.map(
                    (element) => element.layout.top + element.layout.height,
                ),
            );
            const paddingTolerance = 4;
            const precision = this.getInternalRoundingPrecision();
            const sectionHeight = this.roundTo(
                maximumElementBottom +
                    minimumElementTop +
                    2 * this.sectionBorderWidth +
                    paddingTolerance,
                precision,
            );
            this.state.sections[sectionId].layout.height = sectionHeight;
            return sectionHeight;
        },

        getElementsDefaultPositions(elementSizes) {
            const roundToGridSize = (value) =>
                Math.round(value / this.gridSizeInPx) * this.gridSizeInPx + 1;
            const elementPadding = {
                width: roundToGridSize(20),
                height: roundToGridSize(10),
            };
            const pageContentWidth = this.pageContainerSize.width;
            const elementPosition = {
                left: elementPadding.width,
                top: this.gridSizeInPx,
            };
            const elementPositions = [];
            // eslint-disable-next-line unicorn/no-for-loop
            for (let index = 0; index < elementSizes.length; index++) {
                if (
                    elementPosition.left +
                        elementSizes[index].width +
                        elementPadding.width >
                    pageContentWidth
                ) {
                    elementPosition.left = elementPadding.width;
                    elementPosition.top +=
                        elementSizes[index].height + elementPadding.height;
                }
                elementPositions.push({ ...elementPosition });
                elementPosition.left +=
                    elementSizes[index].width + elementPadding.width;
            }
            return elementPositions;
        },

        async loadData(attributes, rowsValues, columnWidths) {
            const headerAttributes = attributes.map((attribute) =>
                this.snakeToSentence(attribute),
            );
            //
            // TODO: remove MAX_COLUMN_NUMBER when good APIs are used
            // const MAX_COLUMN_NUMBER = 5;
            // const MAX_ROW_NUMBER = 3;
            // this.data.pageHeader = attributes.slice(
            //     0,
            //     MAX_COLUMN_NUMBER,
            // );
            // this.data.details = [
            //     firstRowValues.slice(0, MAX_COLUMN_NUMBER),
            // ];
            // this.resetElementsLayoutsAndStyles();
            // attributes.splice(
            //     MAX_COLUMN_NUMBER,
            //     attributes.length - MAX_COLUMN_NUMBER,
            // );
            // attributes = attributes.slice(0, MAX_COLUMN_NUMBER);
            // headerAttributes.splice(
            //     MAX_COLUMN_NUMBER,
            //     headerAttributes.length - MAX_COLUMN_NUMBER,
            // );
            // firstRowValues.splice(
            //     MAX_COLUMN_NUMBER,
            //     firstRowValues.length - MAX_COLUMN_NUMBER,
            // );
            //
            // rowsValues.splice(
            //     this.maximumDetailsPreviewRows,
            //     rowsValues.length - this.maximumDetailsPreviewRows,
            // );
            // for (const rowValues of rowsValues) {
            //     rowValues.splice(
            //         MAX_COLUMN_NUMBER,
            //         rowValues.length - MAX_COLUMN_NUMBER,
            //     );
            // }
            // TODO: remove maximumDetailsPreviewRows or adjust it
            rowsValues = rowsValues.slice(0, this.maximumDetailsPreviewRows);
            // rowsValues = rowsValues.map((rowValues) =>
            //     rowValues.slice(0, MAX_COLUMN_NUMBER),
            // );

            // for (const sectionId in this.state.sections) {
            //     this.deleteSectionElements(sectionId);
            // }
            this.deleteSectionElements('pageHeader');
            this.deleteSectionElements('details');
            // TODO: check if resetEditingState needs to be called
            // this.resetState();
            // TODO: check if nextTick needs to be called
            // await this.$nextTick();

            // Ensure that all elements are deleted
            // for (const elementId in this.state.elements) {
            //     delete this.state.elements[elementId];
            // }

            const addedElementPromises = [];
            const addedElementIDs = [];

            const roundToGridSize = (value) =>
                Math.round(value / this.gridSizeInPx) * this.gridSizeInPx + 1;
            const defaultElementSize = {
                width: roundToGridSize(100),
                height: roundToGridSize(20),
            };
            const elementSizes = attributes.map((_attribute, index) => ({
                width: columnWidths?.[index] ?? defaultElementSize.width,
                height: defaultElementSize.height,
            }));
            const elementPositions =
                this.getElementsDefaultPositions(elementSizes);
            // eslint-disable-next-line unicorn/no-for-loop
            for (let index = 0; index < attributes.length; index++) {
                const headerAttribute = headerAttributes[index];
                const attribute = attributes[index];
                const layout = {
                    ...elementPositions[index],
                    ...elementSizes[index],
                };
                addedElementPromises.push(
                    this.addElementToSection({
                        sectionId: 'pageHeader',
                        text: `${headerAttribute}`,
                        layout,
                    }),
                );
                addedElementIDs.push(`${this.state.maxElementId}`);
                addedElementPromises.push(
                    this.addElementToSection({
                        sectionId: 'details',
                        text: `${attribute}`,
                        layout,
                    }),
                );
                addedElementIDs.push(`${this.state.maxElementId}`);
            }
            for (const elementId of addedElementIDs) {
                this.forceElementsInsideContainer[elementId] = false;
            }
            this.state.sections.details.multiRowsTexts = rowsValues;
            this.$store.formChanged = true;
            await Promise.all(addedElementPromises);
            const oldPageHeaderHeight =
                this.state.sections.pageHeader.layout.height;
            const newPageHeaderHeight =
                this.setSectionHeightToContent('pageHeader');
            this.setSectionHeightToContent('details');
            this.state.sections.details.layout.top +=
                newPageHeaderHeight - oldPageHeaderHeight;
            for (const elementId of addedElementIDs) {
                this.forceElementsInsideContainer[elementId] = true;
            }
        },
        async reload() {
            //
            console.log('apiData');
            if (
                !(await this.confirmDialog(
                    this.$t(
                        'Are you sure you want to reload data, ' +
                            'it will delete existing report definition?',
                    ),
                ))
            ) {
                return;
            }

            if (!this.apiUrl) {
                this.showError(
                    this.$t('The value for Api is not specified for report ') +
                        `"${this.reportName}".`,
                );
                return;
            }
            // const apiOptions = {};
            const apiOptions = {};
            if (this.apiOptions) {
                this.copyObject(this.apiOptions, apiOptions, true);
            }
            // TODO: unify that pars is either always an object or always a string
            if (typeof apiOptions.pars !== 'string') {
                apiOptions.pars = JSON.stringify(apiOptions.pars);
            }
            const offline = false;
            // TODO: optimize loading data so that
            // only necessary data (attributes and first row) is loaded
            // immediately and the rest is loaded after or on demand
            // apiOptions.pars = '{"data_source_id":1}';
            const apiData = await this.get(this.apiUrl, apiOptions, offline);
            // TODO: should be adjusted if custom API format is used
            if (apiData) {
                // this.resetData();
                const [attributes, rowsValues] =
                    this.getAttributesAndRows(apiData);
                await this.loadData(attributes, rowsValues);
            }
        },
        onMenuOpen() {
            // After menu is opened, when click is made outside of the menu
            // menu will close and element and section that were selected
            // before opening the menu will be deselected so
            // watchers are set to prevent that by restoring selection.
            // Watchers are removed after menu is closed.
            // Either when menu option is selected, in that case
            // watchers are removed by onMenuOptionSelected() method
            // before deselection is made so watchers are never called.
            // Or when menu is closed by clicking outside of the menu,
            // in that case watchers should already be removed because
            //  they can be only called once, but in any case
            // method onCloseMenu() will try to remove them also.
            this.savedSelectionData = {
                selectedElementId: this.selectedElementId,
                unwatchElementUnselected: this.$watch(
                    () =>
                        this.editingState.elements[
                            this.savedSelectionData?.selectedElementId
                        ]?.isSelected,
                    async (value) => {
                        if (value || value === undefined) {
                            return;
                        }
                        this.savedSelectionData.unwatchElementUnselected();
                        if (this.isElementSelected || this.isSectionSelected) {
                            return;
                        }
                        await this.$nextTick();
                        this.editingState.elements[
                            this.savedSelectionData.selectedElementId
                        ].isSelected = true;
                    },
                ),
                selectedSectionId: this.selectedSectionId,
                unwatchSectionUnselected: this.$watch(
                    () =>
                        this.editingState.sections[
                            this.savedSelectionData?.selectedSectionId
                        ]?.isSelected,
                    async (value) => {
                        if (value || value === undefined) {
                            return;
                        }
                        this.savedSelectionData.unwatchSectionUnselected();
                        if (this.isElementSelected || this.isSectionSelected) {
                            return;
                        }
                        await this.$nextTick();
                        this.editingState.sections[
                            this.savedSelectionData.selectedSectionId
                        ].isSelected = true;
                    },
                ),
                isMenuOptionSelected: false,
            };
            this.menuOpenedCounter++;
        },

        async onCloseMenu() {
            // if (this.savedSelectionData === null) {
            //     return;
            // }
            this.menuClosedCounter++;
            if (this.menuClosedCounter < this.menuOpenedCounter) {
                return;
            }
            if (this.savedSelectionData.isMenuOptionSelected) {
                return;
            }
            this.savedSelectionData.unwatchElementUnselected();
            this.savedSelectionData.unwatchSectionUnselected();
            // If element or section that was selected when menu was opened
            // is not selected anymore (if it was not reselected by the watchers),
            // then it it will be selected again.
            if (!this.isElementSelected && !this.isSectionSelected) {
                if (this.savedSelectionData.selectedElementId !== null) {
                    this.setSelectedElement(
                        this.savedSelectionData.selectedElementId,
                    );
                } else if (this.savedSelectionData.selectedSectionId !== null) {
                    this.setSelectedSection(
                        this.savedSelectionData.selectedSectionId,
                    );
                }
            }
            this.savedSelectionData = null;
        },
        onMenuOptionSelected() {
            this.savedSelectionData.unwatchElementUnselected();
            this.savedSelectionData.unwatchSectionUnselected();
            this.savedSelectionData.isMenuOptionSelected = true;
        },

        async contentChanged() {
            // TODO: consider optimizing by using elementId to
            // only recalculate layout of the changed element
            this.$store.formChanged = true;
            await this.$nextTick();
            this.recalculateLayoutNumber++;
        },

        // getSectionPositionAndSize(section) {
        //     if (section === undefined) {
        //         return null;
        //     }
        //     const sectionScaledPositionAndSize = section.getPositionAndSize();
        //     const sectionPositionAndSize = Object.fromEntries(
        //         Object.entries(sectionScaledPositionAndSize).map(
        //             ([property, value]) => [property, value / this.zoom],
        //         ),
        //     );
        //     return sectionPositionAndSize;
        // },
        hasLayoutChanged(newValue, oldValue) {
            return ['left', 'top', 'width', 'height'].some(
                (property) => newValue[property] !== oldValue[property],
            );
        },
        setFormChangedWhenLayoutChanged(newValue, oldValue) {
            if (this.$store.formChanged) {
                return;
            }
            if (!this.hasLayoutChanged(newValue, oldValue)) {
                return;
            }
            this.$store.formChanged = true;
        },

        sectionLayoutChanged(newValue, oldValue) {
            this.setFormChangedWhenLayoutChanged(newValue, oldValue);
        },

        fixElementPositionOld(element, left, top) {
            const selectedElement = this.state.elements[this.selectedElementId];
            const roundToGridSize = (value) =>
                Math.round(value / this.gridSizeInPx) * this.gridSizeInPx + 1;
            const elementPadding = {
                width: roundToGridSize(20),
                height: roundToGridSize(10),
            };
            const paddingTolerance = 4;
            const selectedElementRight =
                selectedElement.layout.left + selectedElement.layout.width;
            const selectedElementBottom =
                selectedElement.layout.top + selectedElement.layout.height;
            const right = left + element.layout.width;
            const bottom = top + element.layout.height;
            const paddingWidthOffset = elementPadding.width - paddingTolerance;
            const paddingHeightOffset =
                elementPadding.height - paddingTolerance;
            if (
                left < selectedElementRight + paddingWidthOffset &&
                right + paddingWidthOffset > selectedElement.layout.left &&
                top < selectedElementBottom + paddingHeightOffset &&
                bottom + paddingHeightOffset > selectedElement.layout.top
            ) {
                left = selectedElementRight + elementPadding.width;
            }
            element.layout.left = left;
            element.layout.top = top;
        },

        fixElementPositionsOld(
            sectionId,
            { ignoreSelectedElement = true } = {},
        ) {
            if (this.ignoreFixElementPositions) {
                return;
            }
            this.ignoreFixElementPositions = true;
            const section = this.state.sections[sectionId];
            const elements = section.elements.map(
                (elementId) => this.state.elements[elementId],
            );
            elements.sort((element1, element2) => {
                if (element1.layout.top !== element2.layout.top) {
                    return element1.layout.top - element2.layout.top;
                }
                return element1.layout.left - element2.layout.left;
            });

            const roundToGridSize = (value) =>
                Math.round(value / this.gridSizeInPx) * this.gridSizeInPx + 1;
            const defaultElementSize = {
                width: roundToGridSize(100),
                height: roundToGridSize(20),
            };
            const elementPadding = {
                width: roundToGridSize(20),
                height: roundToGridSize(10),
            };
            const paddingTolerance = 4;
            const selectedElement = this.state.elements[this.selectedElementId];
            let maximumBottom = 0;
            let maximumRowRight = 0;
            let orderChanged = false;
            for (let index = 0; index < elements.length - 1; index++) {
                const element = elements[index];
                const nextElement = elements[index + 1];
                const isElementSelected = element === selectedElement;
                const isNextElementSelected = nextElement === selectedElement;
                if (
                    element.layout.left +
                        element.layout.width +
                        elementPadding.width >
                        section.layout.width &&
                    (!ignoreSelectedElement || !isElementSelected)
                ) {
                    // Element does not fit in the row
                    let top;
                    if (nextElement.layout.top > element.layout.top) {
                        if (
                            nextElement.layout.top <
                            maximumBottom + elementPadding.height
                        ) {
                            top =
                                nextElement.layout.top +
                                nextElement.layout.height +
                                elementPadding.height;
                            orderChanged = true;
                        } else {
                            top = nextElement.layout.top;
                        }
                    } else {
                        top = defaultElementSize.height + elementPadding.height;
                    }
                    this.fixElementPosition(element, elementPadding.width, top);
                }

                if (!ignoreSelectedElement || !isNextElementSelected) {
                    if (
                        nextElement.layout.top <
                        element.layout.top + element.layout.height
                    ) {
                        // Elements should be in same row
                        if (
                            nextElement.layout.left +
                                nextElement.layout.width +
                                elementPadding.width >
                            element.layout.left
                        ) {
                            // Next element is left of the current element
                            const left =
                                element.layout.left +
                                element.layout.width +
                                elementPadding.width;
                            // const top =
                            //     nextElement.layout.top +
                            //         nextElement.layout.height <
                            //     element.layout.top
                            //         ? element.layout.top
                            //         : nextElement.layout.top;
                            // this.fixElementPosition(nextElement, left, top);
                            this.fixElementPosition(
                                nextElement,
                                left,
                                element.layout.top,
                            );
                        }
                    } else {
                        // Elements are in different rows
                        if (
                            element.layout.left +
                                element.layout.width +
                                elementPadding.width +
                                nextElement.layout.width +
                                elementPadding.width <
                                section.layout.width &&
                            (element.layout.top >
                                maximumBottom +
                                    elementPadding.height -
                                    paddingTolerance ||
                                element.layout.left >
                                    maximumRowRight +
                                        elementPadding.width -
                                        paddingTolerance)
                        ) {
                            // Next element fits in the previous row
                            const left =
                                element.layout.left +
                                element.layout.width +
                                elementPadding.width;
                            this.fixElementPosition(
                                nextElement,
                                left,
                                element.layout.top,
                            );
                        } else {
                            const top = Math.max(
                                element.layout.top +
                                    element.layout.height +
                                    elementPadding.height,
                                nextElement.layout.top,
                            );
                            this.fixElementPosition(
                                nextElement,
                                elementPadding.width,
                                top,
                            );
                            maximumRowRight = 0;
                        }
                    }
                }
                maximumBottom = Math.max(
                    maximumBottom,
                    element.layout.top + element.layout.height,
                );
                maximumRowRight = Math.max(
                    maximumRowRight,
                    element.layout.left + element.layout.width,
                );
            }
            // const lastElement = elements.at(-1);
            // if (
            //     lastElement.layout.left +
            //         lastElement.layout.width +
            //         elementPadding.width >
            //         section.layout.width &&
            //     (!ignoreSelectedElement || lastElement !== selectedElement)
            // ) {
            //     lastElement.layout.top +=
            //         defaultElementSize.height + elementPadding.height;
            //     lastElement.layout.left = elementPadding.width;
            // }
            this.setSectionHeightToContent(sectionId);
            if (orderChanged) {
                this.ignoreFixElementPositions = false;
                this.fixElementPositionsOld(sectionId, {
                    ignoreSelectedElement,
                });
            } else {
                this.$nextTick(() => {
                    this.ignoreFixElementPositions = false;
                });
            }
        },

        fixAttributeElementPositions(sectionId) {
            if (this.ignoreFixAttributeElementPositions) {
                return;
            }
            this.ignoreFixAttributeElementPositions = true;
            const section = this.state.sections[sectionId];
            const elements = section.elements.map(
                (elementId) => this.state.elements[elementId],
            );
            const elementSizes = elements.map((element) => ({
                width: element.layout.width,
                height: element.layout.height,
            }));
            const elementPositions =
                this.getElementsDefaultPositions(elementSizes);
            for (const attributeSectionId of ['pageHeader', 'details']) {
                const section = this.state.sections[attributeSectionId];
                for (const [index, elementId] of section.elements.entries()) {
                    const element = this.state.elements[elementId];
                    element.layout.left = elementPositions[index].left;
                    element.layout.top = elementPositions[index].top;
                    element.layout.width = elementSizes[index].width;
                    element.layout.height = elementSizes[index].height;
                }
            }
            const oldPageHeaderHeight =
                this.state.sections.pageHeader.layout.height;
            const newPageHeaderHeight =
                this.setSectionHeightToContent('pageHeader');
            this.setSectionHeightToContent('details');
            this.state.sections.details.layout.top +=
                newPageHeaderHeight - oldPageHeaderHeight;
            this.$nextTick(() => {
                this.ignoreFixAttributeElementPositions = false;
            });
        },

        elementLayoutChanged(newValue, oldValue) {
            this.setFormChangedWhenLayoutChanged(newValue, oldValue);
            if (!this.isElementSelected) {
                return;
            }
            const sectionId =
                this.state.elements[this.selectedElementId].section;
            if (
                this.hasLayoutChanged(newValue, oldValue) &&
                (sectionId === 'details' || sectionId === 'pageHeader')
            ) {
                this.fixAttributeElementPositions(sectionId);
            }

            // const element = this.state.elements[this.selectedElementId];
            // const rightOffset =
            //     newValue.left +
            //     newValue.width -
            //     (oldValue.left + oldValue.width);
            // const section = this.state.sections[element.section];
            // for (const elementId of section.elements) {
            //     const otherElement = this.state.elements[elementId];
            //     const otherElementBottom =
            //         otherElement.layout.top + otherElement.layout.height;
            //     const elementBottom =
            //         element.layout.top + element.layout.height;
            //     if (
            //         otherElement.layout.left > element.layout.left &&
            //         otherElement.layout.top < elementBottom &&
            //         otherElementBottom > element.layout.top
            //     ) {
            //         otherElement.layout.left += rightOffset;
            //     }
            // }
            // const roundToGridSize = (value) =>
            //     Math.round(value / this.gridSizeInPx) * this.gridSizeInPx + 1;
            // const defaultElementSize = {
            //     width: roundToGridSize(100),
            //     height: roundToGridSize(20),
            // };
            // const elementPadding = {
            //     width: roundToGridSize(20),
            //     height: roundToGridSize(10),
            // };
            // for (const elementId of section.elements) {
            //     const otherElement = this.state.elements[elementId];
            //     const otherElementRight =
            //         otherElement.layout.left + otherElement.layout.width;
            //     if (
            //         otherElement !== element &&
            //         otherElementRight > section.layout.width
            //     ) {
            //         // otherElement.layout.width -= rightOffset;
            //         otherElement.layout.top +=
            //             defaultElementSize.height + elementPadding.height;
            //         otherElement.layout.left = elementPadding.width;
            //     }
            // }
        },
        onElementResize(elementId, _size) {
            const element = this.state.elements[elementId];
            if (element.type !== 'image') {
                return;
            }
            element.imageOptions.fit = this.isShiftPressed ? 'contain' : 'fill';
        },
        deselectElementsAndSections() {
            for (const sectionId in this.editingState.sections) {
                this.editingState.sections[sectionId].isSelected = false;
            }
            for (const elementId in this.editingState.elements) {
                this.editingState.elements[elementId].isSelected = false;
            }
        },
        setSelectedElement(selectedElementId) {
            this.deselectElementsAndSections();
            this.editingState.elements[selectedElementId].isSelected = true;
        },
        // onElementSelected(elementId, isSelected) {
        //     // TODO: try to remove this and use only v-model:selected
        //     if (!isSelected && this.selectedElementId !== elementId) {
        //         return;
        //     }
        //     // this.selectedElementId = isSelected ? elementId : null;
        //     if (isSelected) {
        //         this.fontSize =
        //             this.state.elements[elementId].styleData.fontSize ??
        //             this.defaultFontSize;
        //         this.fontFamily =
        //             this.state.elements[elementId].styleData?.fontFamily ??
        //             this.defaultFontFamily;
        //         this.fontBold =
        //             this.state.elements[elementId].styleData.fontWeight ===
        //             'bold';
        //         this.fontItalic =
        //             this.state.elements[elementId].styleData.fontStyle ===
        //             'italic';
        //         this.fontUnderline =
        //             this.state.elements[elementId].styleData.textDecoration ===
        //             'underline';
        //     } else {
        //         this.fontBold = false;
        //         this.fontItalic = false;
        //         this.fontUnderline = false;
        //     }
        // },

        setSelectedSection(sectionId) {
            this.deselectElementsAndSections();
            this.editingState.sections[sectionId].isSelected = true;
        },

        // setSelectedSection(sectionId, isSelected) {
        //     if (!isSelected && this.selectedSectionId !== sectionId) {
        //         return;
        //     }
        //     this.selectedSectionId = isSelected ? sectionId : null;
        //     // TODO: set and reset section properties if needed
        // },

        // onSectionSelected(sectionId, isSelected) {
        //     const x = 1;
        //     // if (
        //     //     this.savedSelectionData !== null &&
        //     //     sectionId === 'documentHeader' &&
        //     //     !isSelected
        //     // ) {
        //     //     this.editingState.sections.documentHeader.isSelected = true;
        //     // }
        // },

        async setSections(sectionIds) {
            const sectionIdsToRemove = Object.keys(this.state.sections).filter(
                (sectionId) => !sectionIds.includes(sectionId),
            );
            const removedSectionHeights = Object.fromEntries(
                sectionIdsToRemove.map((sectionId) => [
                    sectionId,
                    this.state.sections[sectionId].layout.height,
                ]),
            );
            for (const sectionId of sectionIdsToRemove) {
                this.deleteSection(sectionId);
            }
            const sectionIdsToAdd = sectionIds.filter(
                (sectionId) => !(sectionId in this.state.sections),
            );
            const addSectionPromises = [];
            for (const sectionId of sectionIdsToAdd) {
                addSectionPromises.push(this.addSection(sectionId));
            }
            await Promise.all(addSectionPromises);
            // Set sections positions and sizes
            const allSectionIds = Object.keys(this.defaultState.sections);
            for (const removedSectionId of sectionIdsToRemove) {
                const removedSectionIndex =
                    allSectionIds.indexOf(removedSectionId);
                if (removedSectionId.toLowerCase().includes('footer')) {
                    const sectionIdsToMoveDown = allSectionIds
                        .slice(0, removedSectionIndex)
                        .filter((sectionId) =>
                            sectionId.toLowerCase().includes('footer'),
                        );
                    for (const previousSectionId of sectionIdsToMoveDown) {
                        if (previousSectionId in this.state.sections) {
                            this.state.sections[previousSectionId].layout.top +=
                                removedSectionHeights[removedSectionId];
                        }
                    }
                } else {
                    const sectionIdsToMoveUp = allSectionIds
                        .slice(removedSectionIndex + 1)
                        .filter(
                            (sectionId) =>
                                !sectionId.toLowerCase().includes('footer'),
                        );
                    for (const followingSectionId of sectionIdsToMoveUp) {
                        if (followingSectionId in this.state.sections) {
                            this.state.sections[
                                followingSectionId
                            ].layout.top -=
                                removedSectionHeights[removedSectionId];
                        }
                    }
                }
            }
            for (const addedSectionId of sectionIdsToAdd) {
                const addedSectionIndex = allSectionIds.indexOf(addedSectionId);
                if (addedSectionId.toLowerCase().includes('footer')) {
                    const sectionIdsToMoveUp = allSectionIds
                        .slice(0, addedSectionIndex)
                        .filter((sectionId) =>
                            sectionId.toLowerCase().includes('footer'),
                        );
                    for (const previousSectionId of sectionIdsToMoveUp) {
                        if (previousSectionId in this.state.sections) {
                            this.state.sections[previousSectionId].layout.top -=
                                this.state.sections[
                                    addedSectionId
                                ].layout.height;
                        }
                    }
                } else {
                    const sectionIdsToMoveDown = allSectionIds
                        .slice(addedSectionIndex + 1)
                        .filter(
                            (sectionId) =>
                                !sectionId.toLowerCase().includes('footer'),
                        );
                    for (const followingSectionId of sectionIdsToMoveDown) {
                        if (followingSectionId in this.state.sections) {
                            this.state.sections[
                                followingSectionId
                            ].layout.top +=
                                this.state.sections[
                                    addedSectionId
                                ].layout.height;
                        }
                    }
                }
            }
        },

        showSectionsSelect() {
            this.$refs.selectSections.showPopup();
        },

        async addElementToSection({
            sectionId,
            type = 'text',
            text = '',
            imageUrl = '',
            layout = {},
            styleData = {},
        }) {
            if ((type === 'text') | (type === 'page_number')) {
                const defaultStyleData = {
                    fontSize: this.defaultFontSize,
                    fontFamily: this.defaultFontFamily,
                    textAlign: 'center',
                    textWrap: 'wrap',
                    textColor: this.defaultTextColor,
                    textColorRules: [],
                    fillColor: this.defaultFillColor,
                    fillColorRules: [],
                };
                styleData = { ...defaultStyleData, ...styleData };
            }
            this.state.maxElementId++;
            const elementId = `${this.state.maxElementId}`;
            // if (this.isStateSet && (type === 'text' || type === 'image')) {
            if (type === 'text' || type === 'image') {
                this.waitElementsContents[elementId] = true;
            }
            const imageOptions = type === 'image' ? { fit: 'fill' } : {};
            this.state.elements[elementId] = {
                section: sectionId,
                type,
                text,
                imageUrl,
                imageOptions,
                layout,
                styleData,
            };
            this.state.sections[sectionId].elements.push(elementId);
            this.$store.formChanged = true;
            if (this.isStateSet) {
                this.editingState.elements[this.state.maxElementId] = {
                    isSelected: false,
                };
            }
            if (type === 'text') {
                await this.waitForRefs([`elementInput${elementId}`]);
                await this.elementInputChanged(text, elementId, false);
                // await this.$nextTick();

                // await this.$nextTick();
                //
                // await new Promise(window.requestAnimationFrame);
                this.waitElementsContents[elementId] = false;
            }
            // }
        },
        async interactiveAddElementToSection(...arguments_) {
            await this.addElementToSection(...arguments_);
            this.setSelectedElement(this.state.maxElementId);
        },
        async addText() {
            this.onMenuOptionSelected();
            await this.interactiveAddElementToSection({
                sectionId: this.selectedSectionId,
                layout: { width: 50 },
            });
        },
        addImage() {
            this.onMenuOptionSelected();
            this.inAddingImage = true;
        },
        onAddingImageOKClick() {
            this.inAddingImage = false;
            const imageUrl = this.addingImageUrl;
            this.addingImageUrl = '';
            this.interactiveAddElementToSection({
                sectionId: this.selectedSectionId,
                type: 'image',
                imageUrl,
                layout: { width: this.defaultImageWidth, height: 'auto' },
            });
        },
        onAddingImageCancelClick() {
            this.inAddingImage = false;
            this.addingImageUrl = '';
        },
        addPageNumber() {
            this.onMenuOptionSelected();
            this.interactiveAddElementToSection({
                sectionId: this.selectedSectionId,
                type: 'page_number',
                text: '1',
                layout: {
                    left: [0.95, { useRelativeUnits: true }],
                    top: [0.9, { useRelativeUnits: true }],
                },
            });
        },

        async addSection(sectionId) {
            this.state.sections[sectionId] = this.deepClone(
                this.defaultState.sections[sectionId],
            );
            this.editingState.sections[sectionId] = {
                isSelected: false,
            };
            this.$store.formChanged = true;
            return new Promise((resolve) => {
                const unwatch = this.$watch(
                    () => this.state.sections[sectionId].layout,
                    () => {
                        if (
                            ['left', 'top', 'width', 'height'].every(
                                (property) =>
                                    ['number', 'undefined'].includes(
                                        typeof this.state.sections[sectionId]
                                            .layout[property],
                                    ),
                            )
                        ) {
                            unwatch();
                            resolve();
                        }
                    },
                    { immediate: true, deep: true },
                );
            });
        },

        deleteElement(elementId) {
            const sectionId = this.state.elements[elementId].section;
            delete this.state.elements[elementId];
            const elementIndexInSection =
                this.state.sections[sectionId].elements.indexOf(elementId);
            if (elementIndexInSection === -1) {
                return;
            }
            this.state.sections[sectionId].elements.splice(
                elementIndexInSection,
                1,
            );
            delete this.editingState.elements[elementId];
            this.$store.formChanged = true;
        },

        deleteSectionElements(sectionId) {
            for (const elementId of this.state.sections[sectionId].elements) {
                delete this.state.elements[elementId];
                delete this.editingState.elements[elementId];
            }
            this.state.sections[sectionId].elements = [];
            this.$store.formChanged = true;
        },

        deleteSection(sectionId) {
            this.deleteSectionElements(sectionId);
            delete this.state.sections[sectionId];
            delete this.editingState.sections[sectionId];
        },

        deleteSelectedElements() {
            if (this.isSectionSelected) {
                this.deleteSectionElements(this.selectedSectionId);
            }
            if (this.isElementSelected) {
                this.deleteElement(this.selectedElementId);
            }
        },

        alignText(alignment) {
            this.onMenuOptionSelected();
            this.onStylePropertyChange('textAlign', alignment);
        },

        // isAlignActive(alignment) {
        //     const selectedElementStyleData =
        //         this.state.elements?.[this.selectedElementId]?.styleData;
        //     if (selectedElementStyleData === undefined) {
        //         return false;
        //     }
        //     return selectedElementStyleData['textAlign'] === alignment;
        // },

        wrapText(wrapping) {
            this.onMenuOptionSelected();
            this.onStylePropertyChange('textWrap', wrapping);
        },
        chooseTextRuleColor(rule) {
            this.textColorCurrentRule = rule;
            this.textColorChooseRuleColor = true;
        },
        chooseTextDefaultColor() {
            //
            this.textColorChooseDefaultColor = true;
        },
        chooseFillRuleColor(rule) {
            this.fillColorCurrentRule = rule;
            this.fillColorChooseRuleColor = true;
        },
        chooseFillDefaultColor() {
            this.fillColorChooseDefaultColor = true;
        },
        setFillColorChanges(newValue, oldValue) {
            if (this.isSectionSelected) {
                if (this.fillColorChooseRuleColor) {
                    if (this.fillColorCurrentRule === null) {
                        const selectedSectionStyleData =
                            this.state.sections[this.selectedSectionId]
                                .styleData;
                        selectedSectionStyleData.fillColorRules.push({
                            function: this.fillColorNewRuleFunction,
                            color: newValue,
                        });
                        this.fillColorNewRuleFunction = '';
                    } else {
                        this.fillColorCurrentRule.color = newValue;
                        this.fillColorCurrentRule = null;
                    }
                    this.fillColorChooseRuleColor = false;
                    this.fillColor = oldValue;
                    this.$store.formChanged = true;
                    return;
                } else if (this.fillColorChooseDefaultColor) {
                    this.fillColorChooseDefaultColor = false;
                }
                if (
                    this.state.sections[this.selectedSectionId].styleData
                        .fillColor === newValue
                ) {
                    return;
                }
                this.state.sections[
                    this.selectedSectionId
                ].styleData.fillColor = newValue;
                this.contentChanged();
            } else if (this.isElementSelected) {
                if (this.fillColorChooseRuleColor) {
                    if (this.fillColorCurrentRule === null) {
                        const selectedElementStyleData =
                            this.state.elements[this.selectedElementId]
                                .styleData;
                        selectedElementStyleData.fillColorRules.push({
                            function: this.fillColorNewRuleFunction,
                            color: newValue,
                        });
                        this.fillColorNewRuleFunction = '';
                    } else {
                        this.fillColorCurrentRule.color = newValue;
                        this.fillColorCurrentRule = null;
                    }
                    this.fillColorChooseRuleColor = false;
                    this.fillColor = oldValue;
                    this.$store.formChanged = true;
                    return;
                } else if (this.fillColorChooseDefaultColor) {
                    this.fillColorChooseDefaultColor = false;
                }
                this.onStylePropertyChange('fillColor', newValue);
            }
        },
        onFillColorChanged(newValue, oldValue) {
            if (this.isElementSelected && this.isSectionSelected) {
                const unwatch = this.$watch(
                    () => this.isElementSelected && this.isSectionSelected,
                    () => {
                        this.setFillColorChanges(this.fillColor, oldValue);
                        unwatch();
                    },
                );
            } else {
                this.setFillColorChanges(newValue, oldValue);
            }
        },
        adjustMargins(marginsName) {
            this.onMenuOptionSelected();
            if (
                this.equalObjects(
                    this.state.marginsInCm,
                    this.marginValuesInCm[marginsName],
                )
            ) {
                return;
            }
            this.state.marginsInCm = this.marginValuesInCm[marginsName];
            for (const section of Object.values(this.state.sections)) {
                section.layout.width = [1, { useRelativeUnits: true }];
            }
            this.$store.formChanged = true;
        },

        areMarginsActive(marginsInCm) {
            return this.equalObjects(this.state.marginsInCm, marginsInCm);
        },

        setPageOrientation(orientation) {
            //
            this.onMenuOptionSelected();
            if (this.state.pageOrientation === orientation) {
                return;
            }
            this.state.pageOrientation = orientation;
            const pageShorterSize = Math.min(
                this.state.pageSize.width,
                this.state.pageSize.height,
            );
            const pageLongerSize = Math.max(
                this.state.pageSize.width,
                this.state.pageSize.height,
            );
            if (orientation === 'portrait') {
                this.state.pageSize.width = pageShorterSize;
                this.state.pageSize.height = pageLongerSize;
            } else if (orientation === 'landscape') {
                this.state.pageSize.width = pageLongerSize;
                this.state.pageSize.height = pageShorterSize;
            }
            for (const section of Object.values(this.state.sections)) {
                section.layout.width = [1, { useRelativeUnits: true }];
            }
            let footerBottom = this.pageContainerSize.height;
            if ('documentFooter' in this.state.sections) {
                this.state.sections.documentFooter.layout.top =
                    footerBottom -
                    this.state.sections.documentFooter.layout.height;
                footerBottom = this.state.sections.documentFooter.layout.top;
            }
            if ('pageFooter' in this.state.sections) {
                this.state.sections.pageFooter.layout.top =
                    footerBottom -
                    this.state.sections.pageFooter.layout.height;
            }
            if ('details' in this.state.sections) {
                this.fixAttributeElementPositions('details');
            } else if ('pageHeader' in this.state.sections) {
                this.fixAttributeElementPositions('pageHeader');
            }
            this.$store.formChanged = true;
        },

        async elementInputChanged(
            value,
            elementId,
            checkContentChanged = true,
        ) {
            const reference = `elementInput${elementId}`;
            const elementInput =
                this.$refs[reference]?.[0]?.$el?.querySelector('input');
            if (elementInput === undefined) {
                return;
            }
            elementInput.size = Math.max(1, value.length);
            this.$store.formChanged = true;
            await new Promise((resolve) =>
                window.requestAnimationFrame(async () => {
                    await this.wait(100);
                    window.requestAnimationFrame(async () => {
                        await this.wait(100);
                        resolve();
                    });
                }),
            );
            if (checkContentChanged) {
                this.contentChanged();
            }
        },

        onImageLoad(elementId) {
            if (this.state.elements[elementId].type !== 'image') {
                return;
            }
            this.waitElementsContents[elementId] = false;
        },

        //
        // helper methods

        // getInnerSectionSize(layout) {
        getInnerSectionSize(sectionId, sectionWidth, sectionHeight) {
            if (Array.isArray(sectionWidth) || Array.isArray(sectionHeight)) {
                return null;
            }
            // TODO: optimize this so that section size
            // is property and not method
            return this.getSizeWithoutBorder(
                { width: sectionWidth, height: sectionHeight },
                this.sectionBorderWidth,
            );
            // if (layout.width === undefined || layout.height === undefined) {
            //     return null;
            // }
            // return {
            //     width: layout.width,
            //     height: layout.height,
            // };
        },

        getSizeWithoutBorder(size, borderSize) {
            const precision = this.getInternalRoundingPrecision();
            return {
                width: this.roundTo(size.width - borderSize * 2, precision),
                height: this.roundTo(size.height - borderSize * 2, precision),
            };
        },
        setDefaultFontSize(fontSizes) {
            // this.fontSizes[3] is 12
            this.defaultFontSize = fontSizes[3];
        },
        setDefaultFontFamily(fontFamilies) {
            // this.fontFamilies[0] is 'Arial'
            this.defaultFontFamily = fontFamilies[0];
        },

        onStylePropertyChange(propertyName, propertyValue) {
            //
            const selectedElementStyleData =
                this.state.elements?.[this.selectedElementId]?.styleData;
            if (selectedElementStyleData === undefined) {
                return;
            }
            if (selectedElementStyleData[propertyName] === propertyValue) {
                return;
            }
            selectedElementStyleData[propertyName] = propertyValue;
            this.contentChanged();
        },
        async setElementInputSizes() {
            const inputElementsEntires = Object.entries(
                this.state.elements,
            ).filter(([_elementId, element]) => element.type === 'text');
            for (const [elementId, _element] of inputElementsEntires) {
                this.waitElementsContents[elementId] = true;
            }
            const elementInputReferences = inputElementsEntires.map(
                ([elementId, _element]) => `elementInput${elementId}`,
            );
            await this.waitForRefs(elementInputReferences);
            const elementInputSizeChangedPromises = [];
            for (const [elementId, element] of inputElementsEntires) {
                elementInputSizeChangedPromises.push(
                    this.elementInputChanged(element.text, elementId),
                );
                this.waitElementsContents[elementId] = false;
            }
            await Promise.all(elementInputSizeChangedPromises);
        },

        //
        // methods from mixins

        getTop() {
            return this.top;
        },

        getHeaderHeight() {
            return this.headerHeight;
        },

        getPdfReportState() {
            return this.state;
        },

        getStyleRoundingPrecision() {
            return this.styleRoundingPrecision;
        },

        getInternalRoundingPrecision() {
            return this.internalRoundingPrecision;
        },
    },
};
</script>

<style scoped>
.editing-toolbar {
    width: 100%;
    z-index: 100;
}

.editing-toolbar-group {
    padding: 0 10px;
}

.zoom-percentage-input {
    width: 50px;
}

.sections-select {
    position: absolute;
    inset: 0;
}

.font-size-input {
    width: 120px;
}

.font-family-input {
    width: 220px;
}

.menu-button-container {
    padding: 2px;
    min-height: initial;
}

.align-text-item,
.wrap-text-item,
.adjust-margins-item,
.page-orientation-item {
    background-color: rgb(from var(--q-primary) r g b / 12%);
}

.margins-visibility-item {
    padding-top: 0;
    padding-bottom: 0;
}

.editing-q-card {
    overflow: hidden;
}

.editing-container {
    width: 100%;
    height: 100%;
}

.section {
    position: absolute;
    border-style: dashed;
    border-color: black;
}

.editing-main-print-preview-section {
    position: absolute;
}

.section-label {
    position: absolute;
    right: 0;
    padding: 4px 6px;
    color: grey;
    font-size: 12px;
}

@keyframes border-style-animation {
    0% {
        border: 1px dotted transparent;
    }

    100% {
        border: 1px dotted black;
    }
}

@keyframes border-style-reverse-animation {
    0% {
        border: 1px dotted black;
    }

    100% {
        border: 1px dotted transparent;
    }
}

@keyframes border-color-animation {
    0% {
        border: 1px solid transparent;
    }

    100% {
        border: 1px solid black;
    }
}

@keyframes border-color-reverse-animation {
    0% {
        border: 1px solid black;
    }

    100% {
        border: 1px solid transparent;
    }
}

.editing-element {
    border: 1px dotted transparent;
    animation: border-style-animation 0.4s cubic-bezier(0.4, 0, 0.2, 1) forwards;
}

.editing-element-highlighted {
    border: 1px dotted black;
    animation: border-style-reverse-animation 0.4s cubic-bezier(0.4, 0, 0.2, 1)
        forwards;
}

.editing-element::before {
    content: '';
    position: absolute;
    inset: 0;
}

.editing-element-highlighted::before {
    border: 1px solid transparent;
    animation: border-color-animation 0.4s cubic-bezier(0.4, 0, 0.2, 1) forwards;
}

.editing-element:not(.editing-element-highlighted)::before {
    border: 1px solid black;
    animation: border-color-reverse-animation 0.4s cubic-bezier(0.4, 0, 0.2, 1)
        forwards;
}

.add-image-url-input {
    /* TODO: check why does this stylelint error appear */
    /* stylelint-disable-next-line csstree/validator */
    width: min(500px, 70vw);
}
</style>

<style>
.z-max-30000 {
    z-index: 30000;
}

/* stylelint-disable selector-class-pattern */

.__editing_toolbar__q_specifier_for_unscoped_styles__.color-rule-tooltip {
    background-color: rgb(from var(--q-tooltip) r g b / 100%);
}

.__editing_toolbar_input__q_specifier_for_unscoped_styles__
    .zoom-percentage-input
    .q-field__inner {
    display: flex;
    align-items: center;
}

.__editing_toolbar_input__q_specifier_for_unscoped_styles__ .q-field__control {
    height: 30px;
}

.__editing_toolbar_input__q_specifier_for_unscoped_styles__
    .editing-toolbar-input-field {
    text-align: center;
    padding: 6px 0;
}

.__editing_toolbar_input__q_specifier_for_unscoped_styles__
    .font-size-input-field,
.__editing_toolbar_input__q_specifier_for_unscoped_styles__
    .font-family-input-field {
    text-align: left;
}

.__editing_element__q_specifier_for_unscoped_styles__ > .content {
    min-width: fit-content !important;
}

/* TODO: seems to make no difference, remove if unused */

/* .__input__q_specifier_for_unscoped_styles__.q-field {
    min-width: fit-content;
    width: 100%;
} */

.__input__q_specifier_for_unscoped_styles__ .q-field__control,
.__input__q_specifier_for_unscoped_styles__ .q-field--dense .q-field__control {
    height: initial;
    min-height: 100%;
}

.__input__q_specifier_for_unscoped_styles__ .q-field__native {
    line-height: initial;
    padding: 2px 5px;
}

.__input_fit_content__q_specifier_for_unscoped_styles__ .q-field__native {
    width: fit-content;
}

/* stylelint-disable-next-line no-descending-specificity */
.__field__q_specifier_for_unscoped_styles__ .q-field__control,
.__field__q_specifier_for_unscoped_styles__.q-field--dense .q-field__control {
    height: initial;
    min-height: initial;
}

/* stylelint-enable selector-class-pattern */
</style>

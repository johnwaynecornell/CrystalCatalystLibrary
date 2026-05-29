namespace SvgIconAnimation;

public class SvgSrc
{
    public static string svgCatalystRotor = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""128"" height=""128"" viewBox=""0 0 128 128"">
  <defs>
    <radialGradient id=""coreGlow"" cx=""64"" cy=""64"" r=""42"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#ffffff""/>
      <stop offset=""0.22"" stop-color=""#fff4a8""/>
      <stop offset=""0.46"" stop-color=""#ff9d4d""/>
      <stop offset=""0.72"" stop-color=""#a84dff"" stop-opacity=""0.55""/>
      <stop offset=""1"" stop-color=""#00eaff"" stop-opacity=""0""/>
    </radialGradient>

    <linearGradient id=""bladeA"" x1=""64"" y1=""8"" x2=""64"" y2=""64"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#eaffff""/>
      <stop offset=""0.35"" stop-color=""#65f7ff""/>
      <stop offset=""0.72"" stop-color=""#4b9dff""/>
      <stop offset=""1"" stop-color=""#6d4cff""/>
    </linearGradient>

    <linearGradient id=""bladeB"" x1=""64"" y1=""8"" x2=""64"" y2=""64"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#fff7c7""/>
      <stop offset=""0.42"" stop-color=""#ffce62""/>
      <stop offset=""0.75"" stop-color=""#ff7b4a""/>
      <stop offset=""1"" stop-color=""#d74dff""/>
    </linearGradient>

    <linearGradient id=""rim"" x1=""18"" y1=""18"" x2=""110"" y2=""110"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#bfffff""/>
      <stop offset=""0.45"" stop-color=""#5caeff""/>
      <stop offset=""1"" stop-color=""#8a58ff""/>
    </linearGradient>

    <filter id=""softGlow"" x=""-80%"" y=""-80%"" width=""260%"" height=""260%"">
      <feGaussianBlur stdDeviation=""3.2"" result=""blur""/>
      <feMerge>
        <feMergeNode in=""blur""/>
        <feMergeNode in=""SourceGraphic""/>
      </feMerge>
    </filter>

    <filter id=""drop"" x=""-35%"" y=""-35%"" width=""170%"" height=""170%"">
      <feDropShadow dx=""2"" dy=""4"" stdDeviation=""2.2"" flood-color=""#06101e"" flood-opacity=""0.55""/>
    </filter>

    <!-- One rotor blade, reused around center -->
    <path id=""crystalBlade""
          d=""M64 10
             C72 28 77 43 73 56
             C70 65 63 69 56 63
             C48 56 51 44 56 31
             Z""/>

    <!-- Small comet accent, reused around center -->
    <path id=""sparkBlade""
          d=""M64 4
             C68 15 69 24 65 34
             C62 26 58 18 55 10
             Z""/>
  </defs>

  <!-- ambient circular aura -->
  <circle cx=""64"" cy=""64"" r=""53""
          fill=""none""
          stroke=""#44f5ff""
          stroke-width=""5""
          opacity=""0.18""
          filter=""url(#softGlow)""/>

  <circle cx=""64"" cy=""64"" r=""44""
          fill=""url(#coreGlow)""
          opacity=""0.85""
          filter=""url(#softGlow)""/>

  <!-- outer faceted ring -->
  <path d=""M64 7
           L91 17
           L111 37
           L121 64
           L111 91
           L91 111
           L64 121
           L37 111
           L17 91
           L7 64
           L17 37
           L37 17 Z""
        fill=""none""
        stroke=""url(#rim)""
        stroke-width=""4""
        stroke-linejoin=""round""
        opacity=""0.78""
        filter=""url(#drop)""/>

  <!-- four crystal rotor blades -->
  <g filter=""url(#drop)"">
    <use href=""#crystalBlade"" fill=""url(#bladeA)"" stroke=""#07172c"" stroke-width=""2.4"" stroke-linejoin=""round""/>
    <use href=""#crystalBlade"" transform=""rotate(90 64 64)"" fill=""url(#bladeB)"" stroke=""#07172c"" stroke-width=""2.4"" stroke-linejoin=""round""/>
    <use href=""#crystalBlade"" transform=""rotate(180 64 64)"" fill=""url(#bladeA)"" stroke=""#07172c"" stroke-width=""2.4"" stroke-linejoin=""round""/>
    <use href=""#crystalBlade"" transform=""rotate(270 64 64)"" fill=""url(#bladeB)"" stroke=""#07172c"" stroke-width=""2.4"" stroke-linejoin=""round""/>
  </g>

  <!-- secondary diagonal energy fins -->
  <g opacity=""0.78"" filter=""url(#softGlow)"">
    <use href=""#sparkBlade"" fill=""#fff3a2"" transform=""rotate(45 64 64)""/>
    <use href=""#sparkBlade"" fill=""#7ff8ff"" transform=""rotate(135 64 64)""/>
    <use href=""#sparkBlade"" fill=""#fff3a2"" transform=""rotate(225 64 64)""/>
    <use href=""#sparkBlade"" fill=""#7ff8ff"" transform=""rotate(315 64 64)""/>
  </g>

  <!-- facet lines on blades/ring -->
  <g fill=""none"" stroke=""#eaffff"" stroke-width=""1.6"" stroke-linecap=""round"" opacity=""0.55"">
    <path d=""M64 10 L64 62""/>
    <path d=""M64 10 L56 63""/>
    <path d=""M64 10 L73 56""/>

    <path d=""M118 64 L66 64""/>
    <path d=""M118 64 L65 56""/>
    <path d=""M118 64 L72 73""/>

    <path d=""M64 118 L64 66""/>
    <path d=""M64 118 L72 65""/>
    <path d=""M64 118 L55 72""/>

    <path d=""M10 64 L62 64""/>
    <path d=""M10 64 L63 72""/>
    <path d=""M10 64 L56 55""/>
  </g>

  <!-- center catalyst core -->
  <circle cx=""64"" cy=""64"" r=""20""
          fill=""#06172e""
          stroke=""#bfffff""
          stroke-width=""3""
          opacity=""0.92""/>

  <circle cx=""64"" cy=""64"" r=""14""
          fill=""url(#coreGlow)""
          stroke=""#fff6c8""
          stroke-width=""2.2""
          filter=""url(#softGlow)""/>

  <circle cx=""64"" cy=""64"" r=""6""
          fill=""#ffffff""
          opacity=""0.96""/>

  <!-- small asymmetric orbit dots make rotation more readable -->
  <g filter=""url(#softGlow)"">
    <circle cx=""99"" cy=""42"" r=""3.4"" fill=""#fff4a3""/>
    <circle cx=""34"" cy=""91"" r=""2.8"" fill=""#83f8ff""/>
    <circle cx=""88"" cy=""101"" r=""2.2"" fill=""#ff86e8""/>
  </g>
</svg>";
    
    public static string svgCatalystCrystal = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""64"" height=""64"" viewBox=""0 0 64 64"">
  <defs>
    <linearGradient id=""crystalBody"" x1=""4"" y1=""2"" x2=""44"" y2=""56"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#eaffff""/>
      <stop offset=""0.28"" stop-color=""#73f7ff""/>
      <stop offset=""0.62"" stop-color=""#4b9dff""/>
      <stop offset=""1"" stop-color=""#7b4dff""/>
    </linearGradient>

    <linearGradient id=""edgeGlow"" x1=""8"" y1=""4"" x2=""54"" y2=""58"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#ffffff""/>
      <stop offset=""0.45"" stop-color=""#90f8ff""/>
      <stop offset=""1"" stop-color=""#ffc86b""/>
    </linearGradient>

    <radialGradient id=""coreGlow"" cx=""31"" cy=""31"" r=""16"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0"" stop-color=""#ffffff""/>
      <stop offset=""0.28"" stop-color=""#ffe58a""/>
      <stop offset=""0.58"" stop-color=""#ff8a3d""/>
      <stop offset=""1"" stop-color=""#ff4fd8"" stop-opacity=""0""/>
    </radialGradient>

    <filter id=""softGlow"" x=""-60%"" y=""-60%"" width=""220%"" height=""220%"">
      <feGaussianBlur stdDeviation=""2.4"" result=""blur""/>
      <feMerge>
        <feMergeNode in=""blur""/>
        <feMergeNode in=""SourceGraphic""/>
      </feMerge>
    </filter>

    <filter id=""shadow"" x=""-40%"" y=""-40%"" width=""180%"" height=""180%"">
      <feDropShadow dx=""2"" dy=""3"" stdDeviation=""1.8"" flood-color=""#07111f"" flood-opacity=""0.55""/>
    </filter>
  </defs>

  <!-- outer aura -->
  <path d=""M6 4 L45 33 L31 37 L39 56 L30 60 L22 41 L10 52 Z""
        fill=""#42f6ff""
        opacity=""0.22""
        filter=""url(#softGlow)""/>

  <!-- main cursor body -->
  <path d=""M6 4 L45 33 L30 37 L38 56 L30 60 L22 40 L10 51 Z""
        fill=""url(#crystalBody)""
        stroke=""#07172c""
        stroke-width=""3""
        stroke-linejoin=""round""
        filter=""url(#shadow)""/>

  <!-- bright inner face -->
  <path d=""M11 11 L36 31 L25 34 L31 50 L28 52 L21 36 L13 43 Z""
        fill=""#dfffff""
        opacity=""0.38""/>

  <!-- crystal facets -->
  <path d=""M6 4 L25 34 M10 51 L25 34 M45 33 L25 34 M22 40 L30 37""
        fill=""none""
        stroke=""#eaffff""
        stroke-width=""1.6""
        stroke-linecap=""round""
        opacity=""0.68""/>

  <!-- catalyst core glow -->
  <circle cx=""30"" cy=""31"" r=""15""
          fill=""url(#coreGlow)""
          opacity=""0.85""
          filter=""url(#softGlow)""/>

  <!-- catalyst core -->
  <circle cx=""30"" cy=""31"" r=""5.8""
          fill=""#fff6bf""
          stroke=""#ffffff""
          stroke-width=""1.6""/>

  <!-- tiny hot center -->
  <circle cx=""30"" cy=""31"" r=""2.2""
          fill=""#ffffff""/>

  <!-- energy arc -->
  <path d=""M42 13 C51 18 56 27 56 37""
        fill=""none""
        stroke=""url(#edgeGlow)""
        stroke-width=""3""
        stroke-linecap=""round""
        opacity=""0.85""
        filter=""url(#softGlow)""/>

  <path d=""M48 9 L50 15 L56 17 L50 19 L48 25 L46 19 L40 17 L46 15 Z""
        fill=""#fff4a3""
        stroke=""#ffffff""
        stroke-width=""1""
        opacity=""0.95""
        filter=""url(#softGlow)""/>

  <!-- hotspot marker, very subtle: remove if undesired -->
  <circle cx=""6"" cy=""4"" r=""1.4""
          fill=""#ffffff""
          opacity=""0.75""/>
</svg>";
    
    
    
    
}
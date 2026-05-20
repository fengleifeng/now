$root = Join-Path $PSScriptRoot "..\assets\cards"
$ids = Join-Path $root "ids"
$types = Join-Path $root "types"
New-Item -ItemType Directory -Force -Path $ids, $types | Out-Null

function Make-Svg($top, $bottom, $accent, $shape) {
    $shapes = @{
        circle = '<circle cx="48" cy="44" r="22" fill="{0}" opacity="0.92"/>'
        drop = '<path d="M48 22 C58 38 62 48 48 62 C34 48 38 38 48 22 Z" fill="{0}" opacity="0.95"/>'
        leaf = '<path d="M48 24 C62 36 64 52 48 64 C32 52 34 36 48 24 Z" fill="{0}" opacity="0.92"/>'
        flame = '<path d="M48 26 C54 40 58 48 48 66 C38 48 42 40 48 26 Z" fill="{0}"/>'
        gem = '<path d="M48 28 L62 44 L48 60 L34 44 Z" fill="{0}" opacity="0.9"/>'
        fish = '<ellipse cx="48" cy="44" rx="24" ry="14" fill="{0}" opacity="0.9"/><path d="M62 44 L72 38 L72 50 Z" fill="{0}"/>'
        fang = '<path d="M38 30 L48 58 L58 30 Z" fill="{0}" opacity="0.88"/>'
        house = '<path d="M28 50 L48 30 L68 50 V62 H28 Z" fill="{0}" opacity="0.9"/>'
        axe = '<rect x="40" y="28" width="8" height="32" rx="2" fill="#d8c8b0"/><path d="M52 32 L68 28 L64 48 L48 52 Z" fill="{0}"/>'
        cross = '<rect x="44" y="30" width="8" height="28" rx="2" fill="{0}"/><rect x="34" y="40" width="28" height="8" rx="2" fill="{0}"/>'
        map = '<rect x="30" y="32" width="36" height="28" rx="4" fill="{0}" opacity="0.85"/>'
        stack = '<rect x="34" y="40" width="28" height="10" rx="3" fill="{0}"/><rect x="38" y="32" width="20" height="10" rx="3" fill="{0}" opacity="0.75"/>'
        bone = '<ellipse cx="40" cy="44" rx="10" ry="8" fill="{0}"/><ellipse cx="56" cy="44" rx="10" ry="8" fill="{0}"/>'
        cloud = '<ellipse cx="40" cy="46" rx="14" ry="10" fill="{0}"/><ellipse cx="56" cy="44" rx="16" ry="12" fill="{0}" opacity="0.85"/>'
    }
    $body = $shapes[$shape] -f $accent
    @"
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96" width="96" height="96">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="$top"/>
      <stop offset="100%" stop-color="$bottom"/>
    </linearGradient>
  </defs>
  <rect width="96" height="96" rx="14" fill="url(#bg)"/>
  $body
</svg>
"@
}

$cards = @{
    forest=@("#3d6b4f","#1e3d2c","#8fd4a8","map"); lake=@("#3a7ca5","#1a4a6e","#9ed4ff","drop")
    mountain=@("#6b7d8c","#3d4a55","#d0dae4","gem"); plains=@("#7a9e5a","#4a6b38","#c8e89a","leaf")
    cave=@("#5c4f6b","#2e2638","#b8a8d0","gem"); tree=@("#4a7c59","#2a4a35","#a8e0b0","leaf")
    rock=@("#7a7570","#4a4540","#d8d4cf","gem"); bush=@("#5a8f4a","#355f2a","#b8f090","leaf")
    wood=@("#8b5a2b","#5a3818","#e8c090","stack"); stone=@("#8a8580","#555250","#e0dcd6","gem")
    stick=@("#9a7040","#6a4828","#f0d0a0","stack"); plank=@("#a67c52","#6d5238","#ffd8b0","stack")
    iron_ore=@("#6a5a7a","#3e3448","#c8b8e8","gem"); iron_ingot=@("#7a8a9a","#4a5560","#e8f0ff","gem")
    water=@("#4a9fd4","#266080","#b8ecff","drop"); flower=@("#c86a9a","#8a3a62","#ffd0e8","leaf")
    berry=@("#b83c5a","#7a2038","#ff98b0","circle"); herb=@("#4a9a5a","#2a5a38","#b0ffb8","leaf")
    meat=@("#a84a4a","#6a2828","#ffb0a8","bone"); cooked_meat=@("#c86a40","#8a4020","#ffd0a0","bone")
    hide=@("#9a7a5a","#6a5038","#e8d0b0","stack"); rabbit=@("#c8b8a8","#8a7a6a","#fff8f0","circle")
    wolf=@("#6a6a72","#3a3a42","#d0d0e0","fang"); fish=@("#5a9ab8","#2a5a78","#b8f0ff","fish")
    snake=@("#6a9a4a","#3a5a28","#d0ffa8","fang"); bat=@("#4a3a5a","#2a1a38","#c8b0e8","fang")
    axe=@("#8a7a68","#5a4a38","#ffe8c8","axe"); spear=@("#7a8a9a","#4a5a68","#e8f4ff","axe")
    harpoon=@("#6a8aa8","#3a5a78","#d0f0ff","fish"); stone_knife=@("#9a9088","#5a5550","#f0ece8","axe")
    iron_axe=@("#7a8a98","#4a5560","#e0f0ff","axe"); campfire=@("#c86a28","#8a4010","#ffe090","flame")
    trap=@("#8a6a48","#5a4028","#ffd8a8","gem"); tent=@("#c8a060","#8a6838","#fff0c0","house")
    injury=@("#b84a4a","#7a2828","#ffc8c8","cross"); sick=@("#7a9a4a","#4a5a28","#e8ffb0","cross")
    storm=@("#5a6a8a","#2a3a58","#c8d8ff","cloud"); house=@("#a87848","#684828","#ffd8a8","house")
    stone_house=@("#8a8880","#525048","#e8e4dc","house"); bridge=@("#9a7048","#6a4828","#ffd8a8","stack")
    wall=@("#8a8580","#555048","#e0dcd4","gem"); smoke_rack=@("#9a6840","#6a4020","#ffd0a0","flame")
    well=@("#5a8ab0","#2a5878","#b8e8ff","drop"); watchtower=@("#8a7a68","#5a4a38","#ffe8c0","house")
    furnace=@("#b85a30","#7a3818","#ffc890","flame"); workbench=@("#9a7040","#6a4820","#ffd8a0","stack")
}
foreach ($kv in $cards.GetEnumerator()) {
    $t = $kv.Value
    Make-Svg $t[0] $t[1] $t[2] $t[3] | Set-Content -Path (Join-Path $ids "$($kv.Key).svg") -Encoding UTF8
}
$typeCards = @{
    location=@("#3d6b5a","#1e3d30","#9ed8b8","map"); resource=@("#8a6a40","#5a4028","#ffd8a0","stack")
    creature=@("#6a5a7a","#3a2a48","#d8c8f0","circle"); tool=@("#6a7a8a","#3a4a58","#d0e8ff","axe")
    weapon=@("#8a4a4a","#5a2828","#ffc8c8","axe"); building=@("#9a7048","#6a4828","#ffe0b0","house")
    status=@("#9a4a5a","#6a2838","#ffc0d0","cross"); event=@("#5a6a9a","#2a3a68","#c8d8ff","cloud")
    container=@("#7a6a58","#4a3828","#e8d8c0","stack"); seed=@("#5a9a48","#2a5a28","#c0ffa8","leaf")
}
foreach ($kv in $typeCards.GetEnumerator()) {
    $t = $kv.Value
    Make-Svg $t[0] $t[1] $t[2] $t[3] | Set-Content -Path (Join-Path $types "$($kv.Key).svg") -Encoding UTF8
}
Write-Host "Generated $($cards.Count) id + $($typeCards.Count) type SVGs"

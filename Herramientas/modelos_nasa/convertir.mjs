// Convierte los modelos de NASA 3D Resources (GLB con Draco / STL) a OBJ+MTL+PNG que Unity importa sin paquetes.
// Uso: node convertir.mjs entrada.(glb|stl) carpetaSalida nombre [triangulosMax] [tamano]
//  - Centra el modelo y lo escala para que su lado mayor mida `tamano` (2 por defecto).
//  - Simplifica con meshoptimizer si pasa de triangulosMax.
import fs from 'fs';
import path from 'path';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import draco3d from 'draco3dgltf';
import { MeshoptSimplifier } from 'meshoptimizer';

const [entrada, salida, nombre, maxTriArg, tamArg, zArriba] = process.argv.slice(2);
const maxTri = Number(maxTriArg || 30000);
const tamano = Number(tamArg || 2);
fs.mkdirSync(salida, { recursive: true });
await MeshoptSimplifier.ready;

// grupos: { material, color[4], textura(png Buffer|null), pos[], nor[], uv[], idx[] }
const grupos = [];

function mul(m, v, w) { // matriz columna-mayor 4x4
  return [m[0]*v[0]+m[4]*v[1]+m[8]*v[2]+m[12]*w, m[1]*v[0]+m[5]*v[1]+m[9]*v[2]+m[13]*w, m[2]*v[0]+m[6]*v[1]+m[10]*v[2]+m[14]*w];
}

if (entrada.endsWith('.glb')) {
  const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({
    'draco3d.decoder': await draco3d.createDecoderModule(),
  });
  const doc = await io.read(entrada);
  const root = doc.getRoot();
  const porMaterial = new Map();
  for (const node of root.listNodes()) {
    const mesh = node.getMesh(); if (!mesh) continue;
    const m = node.getWorldMatrix();
    for (const prim of mesh.listPrimitives()) {
      if (prim.getMode() !== 4) continue;
      const mat = prim.getMaterial();
      const clave = mat ? mat.getName() || 'mat' : 'sin';
      if (!porMaterial.has(clave)) {
        let png = null;
        const tex = mat && mat.getBaseColorTexture();
        if (tex) {
          // La textura principal puede ser WebP; el GLB trae una copia PNG con el mismo nombre.
          if (tex.getMimeType() === 'image/png') png = tex.getImage();
          else {
            // Con EXT_texture_webp las imágenes vienen en pares (webp, png): la PNG es la siguiente.
            const lista = root.listTextures(); const sig = lista[lista.indexOf(tex) + 1];
            const alt = sig && sig.getMimeType() === 'image/png' ? sig : null;
            png = alt ? alt.getImage() : null;
            if (!png) console.warn('  sin PNG para', tex.getName());
          }
        }
        const g = { material: clave.replace(/[^A-Za-z0-9_]/g, '_'), color: mat ? mat.getBaseColorFactor() : [0.8,0.8,0.8,1], png, metal: mat ? mat.getMetallicFactor() : 0, rough: mat ? mat.getRoughnessFactor() : 0.6, pos: [], nor: [], uv: [], idx: [] };
        porMaterial.set(clave, g); grupos.push(g);
      }
      const g = porMaterial.get(clave);
      const P = prim.getAttribute('POSITION'), N = prim.getAttribute('NORMAL'), T = prim.getAttribute('TEXCOORD_0');
      const base = g.pos.length / 3;
      const v = [0,0,0], n = [0,0,0], t = [0,0];
      for (let i = 0; i < P.getCount(); i++) {
        P.getElement(i, v); g.pos.push(...mul(m, v, 1));
        if (N) { N.getElement(i, n); const r = mul(m, n, 0); const l = Math.hypot(...r) || 1; g.nor.push(r[0]/l, r[1]/l, r[2]/l); } else g.nor.push(0,1,0);
        if (T) { T.getElement(i, t); g.uv.push(t[0], 1 - t[1]); } else g.uv.push(0, 0);
      }
      const I = prim.getIndices();
      if (I) for (let i = 0; i < I.getCount(); i++) g.idx.push(base + I.getScalar(i));
      else for (let i = 0; i < P.getCount(); i++) g.idx.push(base + i);
      // Determinante negativo: invierte el orden de los triángulos.
      const det = m[0]*(m[5]*m[10]-m[6]*m[9]) - m[4]*(m[1]*m[10]-m[2]*m[9]) + m[8]*(m[1]*m[6]-m[2]*m[5]);
      if (det < 0) for (let k = g.idx.length - (I ? I.getCount() : P.getCount()); k < g.idx.length; k += 3) { const a = g.idx[k+1]; g.idx[k+1] = g.idx[k+2]; g.idx[k+2] = a; }
    }
  }
} else {
  // STL binario: soldar vértices y calcular normales suaves.
  const b = fs.readFileSync(entrada);
  const n = b.readUInt32LE(80);
  const mapa = new Map(); const pos = []; const idx = [];
  for (let i = 0; i < n; i++) {
    const o = 84 + i * 50 + 12;
    for (let k = 0; k < 3; k++) {
      const x = b.readFloatLE(o + k*12), y = b.readFloatLE(o + k*12 + 4), z = b.readFloatLE(o + k*12 + 8);
      const key = Math.round(x*1e4) + ',' + Math.round(y*1e4) + ',' + Math.round(z*1e4);
      let id = mapa.get(key);
      if (id === undefined) { id = pos.length / 3; mapa.set(key, id); pos.push(x, y, z); }
      idx.push(id);
    }
  }
  // STL suele venir con Z hacia arriba: se pasa a Y hacia arriba.
  if (zArriba !== 'y') for (let i = 0; i < pos.length; i += 3) { const y = pos[i+1]; pos[i+1] = pos[i+2]; pos[i+2] = -y; }
  if (nombre.includes('SLS')) {
    // El SLS impreso viene en un solo color: se separa por zonas para pintarlo como el real.
    // Eje vertical Y; la etapa central es lo que está cerca del eje y por debajo del ~66 % de la altura.
    let minY = Infinity, maxY = -Infinity;
    for (let i = 1; i < pos.length; i += 3) { minY = Math.min(minY, pos[i]); maxY = Math.max(maxY, pos[i]); }
    const H = maxY - minY;
    // Eje: centro de la caja en X (propulsores simétricos); en Z, centro de la etapa central en la franja media.
    let minX = Infinity, maxX = -Infinity;
    for (let i = 0; i < pos.length; i += 3) { minX = Math.min(minX, pos[i]); maxX = Math.max(maxX, pos[i]); }
    const ax = (minX + maxX) / 2;
    let zMin = Infinity, zMax = -Infinity;
    for (let i = 0; i < pos.length; i += 3) { const h = (pos[i+1]-minY)/H; if (h > 0.3 && h < 0.5 && Math.abs(pos[i]-ax) < 0.01 * H) { zMin = Math.min(zMin, pos[i+2]); zMax = Math.max(zMax, pos[i+2]); } }
    const az = (zMin + zMax) / 2;
    console.log('  eje', ax.toFixed(1), az.toFixed(1), 'diámetro núcleo en Z', (zMax - zMin).toFixed(1), 'de', H.toFixed(1));
    const radioNucleo = 4.6 / 98 * H;    // 8.4 m de diámetro sobre 98 m de alto, con margen
    const altoNucleo = 0.665 * H;
    const naranja = [], blanco = [], oscuro = [];
    for (let k = 0; k < idx.length; k += 3) {
      let cx = 0, cy = 0, cz = 0;
      for (let j = 0; j < 3; j++) { cx += pos[idx[k+j]*3]; cy += pos[idx[k+j]*3+1]; cz += pos[idx[k+j]*3+2]; }
      cx /= 3; cy /= 3; cz /= 3;
      const h = (cy - minY) / H, r = Math.hypot(cx - ax, cz - az);
      const tri = [idx[k], idx[k+1], idx[k+2]];
      if (h < 0.035) oscuro.push(...tri);                                   // toberas
      else if (r < radioNucleo && h > 0.06 && h < altoNucleo / H) naranja.push(...tri);
      else if (r >= radioNucleo && h < 0.06) oscuro.push(...tri);           // faldones de los propulsores
      else blanco.push(...tri);
    }
    grupos.push({ material: 'naranja', color: [0.78,0.40,0.12,1], png: null, pos, nor: null, uv: null, idx: naranja });
    grupos.push({ material: 'blanco', color: [0.92,0.93,0.94,1], png: null, pos, nor: null, uv: null, idx: blanco });
    grupos.push({ material: 'oscuro', color: [0.16,0.17,0.19,1], png: null, pos, nor: null, uv: null, idx: oscuro });
  } else
    grupos.push({ material: 'gris', color: [0.82,0.82,0.80,1], png: null, metal: 0, rough: 0.7, pos, nor: null, uv: null, idx });
}

// Simplificación global proporcional por grupo.
let total = grupos.reduce((s, g) => s + g.idx.length / 3, 0);
console.log(nombre, 'triángulos originales', total);
if (total > maxTri) {
  const f = maxTri / total;
  for (const g of grupos) {
    const objetivo = Math.max(3, Math.floor(g.idx.length * f / 3) * 3);
    const res = MeshoptSimplifier.simplify(new Uint32Array(g.idx), new Float32Array(g.pos), 3, objetivo, 0.02, ['LockBorder']);
    let out = res[0];
    if (out.length > objetivo * 1.3) out = MeshoptSimplifier.simplifySloppy(new Uint32Array(g.idx), new Float32Array(g.pos), 3, null, objetivo, 0.05)[0];
    g.idx = Array.from(out);
  }
  total = grupos.reduce((s, g) => s + g.idx.length / 3, 0);
  console.log('  simplificado a', total);
}

// Normales para STL (suaves por vértice).
for (const g of grupos) if (!g.nor) {
  const comp = grupos.find(o => o !== g && o.pos === g.pos && o.nor); if (comp) { g.nor = comp.nor; continue; }
  const todos = grupos.filter(o => o.pos === g.pos).flatMap(o => o.idx);
  g.nor = new Array(g.pos.length).fill(0);
  for (let k = 0; k < todos.length; k += 3) {
    const [a, b2, c] = [todos[k], todos[k+1], todos[k+2]];
    const ax = g.pos[b2*3]-g.pos[a*3], ay = g.pos[b2*3+1]-g.pos[a*3+1], az = g.pos[b2*3+2]-g.pos[a*3+2];
    const bx = g.pos[c*3]-g.pos[a*3], by = g.pos[c*3+1]-g.pos[a*3+1], bz = g.pos[c*3+2]-g.pos[a*3+2];
    const nx = ay*bz-az*by, ny = az*bx-ax*bz, nz = ax*by-ay*bx;
    for (const v of [a, b2, c]) { g.nor[v*3] += nx; g.nor[v*3+1] += ny; g.nor[v*3+2] += nz; }
  }
  for (let i = 0; i < g.nor.length; i += 3) { const l = Math.hypot(g.nor[i], g.nor[i+1], g.nor[i+2]) || 1; g.nor[i] /= l; g.nor[i+1] /= l; g.nor[i+2] /= l; }
}

// Centro y escala (solo vértices usados).
const min = [Infinity, Infinity, Infinity], max = [-Infinity, -Infinity, -Infinity];
for (const g of grupos) for (const v of g.idx) for (let k = 0; k < 3; k++) { min[k] = Math.min(min[k], g.pos[v*3+k]); max[k] = Math.max(max[k], g.pos[v*3+k]); }
const centro = min.map((x, k) => (x + max[k]) / 2);
const escala = tamano / Math.max(max[0]-min[0], max[1]-min[1], max[2]-min[2]);
console.log('  tamaño original', max.map((x, k) => (x - min[k]).toFixed(2)).join(' x '));

// OBJ: compacta los vértices usados de cada grupo. Con CORTE (fracción de altura), escribe además dos
// archivos _Inferior y _Superior separados por la altura del centro de cada triángulo (separación de etapas).
function escribir(nombreArchivo, filtro) {
  let obj = `# ${nombreArchivo}: convertido de NASA 3D Resources (https://science.nasa.gov/3d-resources/)\nmtllib ${nombreArchivo}.mtl\n`;
  let mtl = '';
  let offset = 1;
  for (const g of grupos) {
    const idx = [];
    for (let k = 0; k < g.idx.length; k += 3) if (!filtro || filtro(g, k)) idx.push(g.idx[k], g.idx[k+1], g.idx[k+2]);
    if (idx.length === 0) continue;
    const remap = new Map(); const vs = [];
    for (const v of idx) if (!remap.has(v)) { remap.set(v, vs.length); vs.push(v); }
    obj += `o ${g.material}\nusemtl ${g.material}\n`;
    for (const v of vs) obj += `v ${((g.pos[v*3]-centro[0])*escala).toFixed(5)} ${((g.pos[v*3+1]-centro[1])*escala).toFixed(5)} ${((g.pos[v*3+2]-centro[2])*escala).toFixed(5)}\n`;
    for (const v of vs) obj += `vn ${g.nor[v*3].toFixed(4)} ${g.nor[v*3+1].toFixed(4)} ${g.nor[v*3+2].toFixed(4)}\n`;
    const conUv = !!g.uv;
    if (conUv) for (const v of vs) obj += `vt ${g.uv[v*2].toFixed(5)} ${(1 - g.uv[v*2+1]).toFixed(5)}\n`;
    for (let k = 0; k < idx.length; k += 3) {
      const f = [0,1,2].map(j => remap.get(idx[k+j]) + offset);
      obj += conUv ? `f ${f[0]}/${f[0]}/${f[0]} ${f[1]}/${f[1]}/${f[1]} ${f[2]}/${f[2]}/${f[2]}\n` : `f ${f[0]}//${f[0]} ${f[1]}//${f[1]} ${f[2]}//${f[2]}\n`;
    }
    offset += vs.length;
    mtl += `newmtl ${g.material}\nKd ${g.color.slice(0,3).map(x => x.toFixed(4)).join(' ')}\n`;
    if (g.png) {
      const archivo = `${nombre}_${g.material}.png`;
      fs.writeFileSync(path.join(salida, archivo), g.png);
      mtl += `map_Kd ${archivo}\n`;
    }
    mtl += '\n';
  }
  fs.writeFileSync(path.join(salida, nombreArchivo + '.obj'), obj);
  fs.writeFileSync(path.join(salida, nombreArchivo + '.mtl'), mtl);
  console.log('  ->', nombreArchivo + '.obj', (obj.length / 1e6).toFixed(2), 'MB');
}

const corte = process.env.CORTE ? Number(process.env.CORTE) : null;
if (corte === null) escribir(nombre, null);
else {
  const yCorte = min[1] + (max[1] - min[1]) * corte;
  const alto = (g, k) => (g.pos[g.idx[k]*3+1] + g.pos[g.idx[k+1]*3+1] + g.pos[g.idx[k+2]*3+1]) / 3;
  escribir(nombre + '_Inferior', (g, k) => alto(g, k) < yCorte);
  escribir(nombre + '_Superior', (g, k) => alto(g, k) >= yCorte);
}

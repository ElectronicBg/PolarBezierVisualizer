# Polar Bézier Curve Visualizer  
## <a href="https://files.fm/u/tqsemqyhyw">Download the simulation</a>

<p align="center">
  <img src="Images/img1.png" width="45%">
  <img src="Images/img2.png" width="45%">
</p>

<p align="center">
  <i>Left: Bézier curve defined in polar coordinates.  
  Right: the polar curve of the Bézier.</i>
</p>

**Polar Bézier Curve Visualizer** is an interactive 2D Unity tool for designing, exploring, and analyzing Bézier curves defined in **polar coordinates**, including real-time **De Casteljau subdivision** and the **polar curve of a Bézier curve**.

This project was developed as part of the course  
**Computer Geometric Modeling**  
at the **Faculty of Mathematics and Informatics, Sofia University “St. Kliment Ohridski”** 🎓  

It combines **computer graphics**, **computational geometry**, and **projective geometry** into a real-time interactive curve laboratory.

---

# 🎮 Controls (How to use)

| Action | Control |
|------|--------|
| Pan camera | Middle mouse or right mouse drag |
| Zoom | Mouse wheel |
| Frame curve | F button |
| Move control point | Left mouse drag |
| Change parameter `t` | Slider |
| Add / remove control points | Point count slider |
| Show / hide polar curve | Toggle button |
| Open menu | Escape |
| Exit | Escape → Exit |

---

# ✨ What this project is

This system behaves like a small curve editor similar to vector graphics or CAD tools, but focused on **geometric understanding** rather than pure drawing.

It allows you to:

- Define Bézier curves using **polar control points**
- Drag control points in real time
- Add and remove points dynamically
- Visualize the **control polygon**
- Evaluate Bézier curves using **De Casteljau’s algorithm**
- Construct **derived Bézier curves**
- Explore the **polar curve of a Bézier curve**

---

# 🧭 Polar Bézier curves

Instead of Cartesian control points (x, y), we use **polar coordinates**:

    Pi = (ri, θi)

Each control point is converted to world space by:

    xi = ri · cos(θi)
    yi = ri · sin(θi)

and then shifted by the polar origin O:

    Pi(world) = (xi, yi) + O

This representation allows:

- Easy rotation (change θ)
- Radial scaling (change r)
- Circular and spiral shapes
- Strong geometric symmetry

---

# 📐 Bézier curve definition

A Bézier curve defined by control points P0 … Pn is:

    C(t) = Σ (from i = 0 to n) [ Bi,n(t) · Pi ]

where the Bernstein polynomials are:

    Bi,n(t) = (n choose i) · (1 − t)^(n − i) · t^i

---

# 🔁 De Casteljau algorithm

Instead of evaluating the Bézier polynomial directly, the system uses the **De Casteljau algorithm**.

Start from:

    Pi(0) = Pi

and recursively interpolate:

    Pi(k)(t) = (1 − t) · Pi(k−1) + t · P(i+1)(k−1)

After n levels:

    C(t) = P0(n)(t)

This gives the exact point on the Bézier curve.

---

# 🔀 Derived Bézier & Polar curve

The first De Casteljau level is:

    Pi(1)(t) = (1 − t) · Pi + t · P(i+1)

These points define a **new Bézier curve of degree n−1**.

In projective geometry, the **polar of a point Q** with respect to a Bézier curve C is the set of points on C whose tangent lines pass through Q.

For Bézier curves:

    If C has degree n
    the polar has degree n−1

The project visualizes this polar curve in real time.

---

# 🧠 Smart point management

When adding a new control point, the system:

1. Converts the control polygon to world space  
2. Finds the longest segment  
3. Inserts the new point at its midpoint  

When removing points, **FIFO order** is used:

    First added point → first removed

This keeps the curve stable and predictable while preserving the endpoints.

---

# 🖥️ Rendering

The curve resolution adapts to screen size and curvature:

High-curvature regions automatically receive more segments for smoothness.

Sorting layers ensure the Bézier curve is always rendered above the polar grid.

---

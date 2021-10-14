#version 450 core

in vec3 in_position;

void main()
{
    // Set where to render the vertex
    gl_Position = vec4(in_position, 1.0);
}

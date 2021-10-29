#version 450 core

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec4 a_color;
layout(location = 2) in float a_batchIndex;

uniform mat4 u_transform[${{ BATCH_SIZE }}];

out vec4 rect_color;

void main()
{
    int index = int(a_batchIndex);

    rect_color = a_color;

    gl_Position = vec4(a_position, 1.0) * u_transform[index];
}

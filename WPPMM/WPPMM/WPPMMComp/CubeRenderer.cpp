#include "pch.h"
#include "CubeRenderer.h"
#include "DDSTextureLoader.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;

CubeRenderer::CubeRenderer()
{

}

void CubeRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();
	
	spriteBatch = std::unique_ptr<DirectX::SpriteBatch>(new DirectX::SpriteBatch(m_d3dContext.Get()));

}

void CubeRenderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();


}

void CubeRenderer::Update(float timeTotal, float timeDelta)
{

}

void CubeRenderer::Render()
{    // clear
	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	const float midnightBlue[] = { 0.098f, 0.098f, 0.439f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		midnightBlue
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);


	spriteBatch->Begin();


	spriteBatch->End();
}

void CubeRenderer::setScreenInformation(byte *data, int *size){
	this->screen_data = data;
	this->screen_data_size = size;

	memcpy_s(screen_buf, SCREEN_BUF_SIZE, data, *size);
}
